#if (TestCrud || TestUpdate)
#define CreateEnabled
#define AskEnabled
#define AddEnabled
#define UpdateEnabled
#elif (TestCreate)
#define CreateEnabled
#elif (TestAsk)
#define CreateEnabled
#define AskEnabled
#elif (TestAdd)
#define CreateEnabled
#define AskEnabled
#define AddEnabled
#endif

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;

namespace PQnA.Test
{
    [TestClass()]
    public class UnitTest1
    {
        public static Program program;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            program = new Program();
        }

#if (CreateEnabled)
        //
        // Create QnA knowledge base.
        // Will contain one Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> CreateDatabase()
        {
            string createFaq = "..\\..\\..\\..\\Data\\create-faq.csv";
            bool result - await program.CreateQnA(createFaq);
        }
#endif
#if (AskEnabled)
        //
        // Create QnA knowledge base.
        // Will contain one Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> AskQuestion(bool production)
        {
        }
#endif
#if (AddEnabled && AskEnabled)
        //
        // Add an additional Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> AddQuestion(bool production)
        {
        }
#endif
#if (UpdateEnabled && AskEnabled)
        //
        // Update the existing two quesions to remove temporary text. 
        private async Task<bool> UpdateQuestions(bool production)
        {
        }
#endif
#if (AddEnabled && AskEnabled)
//
        // Add full text, will add additional questions to the first two entries.
        private async Task<bool> AddFullText(bool production)
        {
        }
#endif
#if (TestCreate)
        [TestMethod()]
        public void TestCreate()
        {
            try
            {
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
            }   
            finally
            {
                if (service.KnowledgeBaseID != null && service.KnowledgeBaseID.Length > 0)
                {
                    Task.Run(async () => { Assert.IsTrue(await CleanUp()); }).Wait();
                }
            }
        }     
#endif
#if (TestAsk)
        [TestMethod()]
        public void TestCreate()
        {
            try
            {
                bool production = false;
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AskQuestion(production)); }).Wait();
            }
            finally
            {
                if (service.KnowledgeBaseID != null && service.KnowledgeBaseID.Length > 0)
                {
                    Task.Run(async () => { Assert.IsTrue(await CleanUp()); }).Wait();
                }
            }
        }
#endif
#if (TestAdd)
        [TestMethod()]
        public void TestAdd()
        {
            try
            {
                bool production = false;
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AskQuestion(production)); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AddQuestion(production)); }).Wait();
            }   
            finally
            {
                if (service.KnowledgeBaseID != null && service.KnowledgeBaseID.Length > 0)
                {
                    Task.Run(async () => { Assert.IsTrue(await CleanUp()); }).Wait();
                }
            }
        }     
#endif
#if (TestUpdate)
        [TestMethod()]
        public void TestUpdate()
        {
            try
            {
                bool production = false;
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AskQuestion(production)); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AddQuestion(production)); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await UpdateQuestions(production)); }).Wait();
            }   
            finally
            {
                if (service.KnowledgeBaseID != null && service.KnowledgeBaseID.Length > 0)
                {
                    Task.Run(async () => { Assert.IsTrue(await CleanUp()); }).Wait();
                }
            }
        }     
#endif
#if (TestCrud)
        [TestMethod()]
        public void CrudTest()
        {
            try
            {
                bool production = true;
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await PublishDatabase());  }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AskQuestion(production)); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AddQuestion(production)); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await UpdateQuestions(production)); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AddFullText(production)); }).Wait();
            }   
            finally
            {
                if (service.KnowledgeBaseID != null && service.KnowledgeBaseID.Length > 0)
                {
                    Task.Run(async () => { Assert.IsTrue(await CleanUp()); }).Wait();
                }
            }
        }

#endif

    }
}
