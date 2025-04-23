using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Tebot{
    public static class TebotHostBuilder{
        public static HostApplicationBuilder CreateBotApplication(string jsonConfigFile = "config.json", string[] commandLineArgs = null, System.Type stateImplementation = null, StateLoader stateLoader = null){
            var bld = Host.CreateApplicationBuilder();

            bld.Configuration.AddJsonFile(jsonConfigFile, true);
            bld.Configuration.AddEnvironmentVariables();

            if(commandLineArgs!=null){
                bld.Configuration.AddCommandLine(commandLineArgs);
            }

            bld.Logging.AddConsole();

            Type finalType = stateImplementation;

            //пытаемся найти имплементацию базового класса в вызывающей сборке и присвоить ее если явно не указанно. если найдено несколько, то выбрасывается исключение
            if(stateImplementation == null){
                var assmbl = Assembly.GetCallingAssembly();
                Type targetState = null;
                foreach(var definedType in assmbl.DefinedTypes){
                    Type tmp = definedType.AsType();
                    if(isTebotBaseDerived(tmp)){
                        if(targetState == null){
                            targetState = tmp;
                        }
                        else{
                            throw new Exception("More that one class, derived from Tebot.Base. Please, set you implementation clearly.");
                        }
                    }
                }
                if(targetState == null){
                    throw new Exception("implementation class are not detected.");
                }
                else{
                    finalType = targetState;
                }
            }

            bld.Services.AddHostedService<Tebot>((provider)=>{
                string token = bld.Configuration.GetValue<string>("token") ?? null;
                if(string.IsNullOrEmpty(token)){
                    throw new Exception("Bot token are not set.");
                }
                StateLoader loader = stateLoader;
                if(loader == null){
                    loader = StateLoader.Empty();
                }
                return new Tebot(bld.Configuration.GetValue<string>("token"), finalType, loader, serviceProvider: provider);
            });

            return bld;
        }

        internal static bool isTebotBaseDerived(Type type){
            if(type.BaseType == typeof(Base)){
                return true;
            }
            if(type.BaseType == typeof(Object)){
                return false;
            }
            return isTebotBaseDerived(type.BaseType);
        }
    }
}