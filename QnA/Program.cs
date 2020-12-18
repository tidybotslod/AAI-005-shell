using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;

namespace QnA
{
    public class Program
    {
        static void Main(string[] args)
        {

            Program program = new Program();

            var rootCommand = new RootCommand
            {

            };

            rootCommand.Description = "";
            
            rootCommand.Handler = CommandHandler.Create(() =>
            {

            });

            rootCommand.InvokeAsync(args).Wait();

        }

    }
}
