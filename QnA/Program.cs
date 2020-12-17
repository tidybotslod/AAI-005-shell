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
        QnAService QnA;

        public Program()
        {
            QnA = new QnAService();
        }

        public QnAService Service { get { return QnA; } }

        public async Task<bool> CreateQnA(string file)
        {
            return false;
        }

        public async Task<bool> AddToQnA(string file, bool published)
        {
            return false;
        }

        public async Task<bool> UpdateQnA(string file, bool published)
        {
            return false;
        }

        public async Task Train(string file, bool published)
        {
        }
        public async Task Publish()
        {
        }

        public async Task Ask(string question, bool published, int number)
        {
        }
        public async Task DeleteKnowledgeBase()
        {
            await QnA.DeleteKnowledgeBase();
        }

        static void Main(string[] args)
        {

            Program program = new Program();

            var rootCommand = new RootCommand
            {

            };
            rootCommand.Handler = CommandHandler.Create(() =>
            {

            });

            rootCommand.InvokeAsync(args).Wait();

        }

    }
}
