#if (TestCreate)
#define CreateEnabled
#elif (TestAsk)
#define CreateEnabled
#define AskEnabled
#elif (TestAdd)
#define AddEnabled
#define AskEnabled
#elif (TestUpdate)
#define UpdateEnabled
#define AskEnabled
#elif (TestPublish)
#define PublishEnabled
#define AskEnabled
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
            bool result = await program.CreateQnA(createFaq);
            Assert.IsTrue(result);
            return result;
        }
#endif
#if (AskEnabled)
        //
        // Create QnA knowledge base.
        // Will contain one Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> AskQuestion(bool production = false)
        {
            int answers = await program.Ask("HOW CAN I CHANGE MY SHIPPING ADDRESS", production, 8);
            bool result = answers > 0;
            Assert.IsTrue(result);
            return result;
        }
#endif
#if (AddEnabled)
        //
        // Add an additional Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> AddQuestions()
        {
            string addFaq = "..\\..\\..\\..\\Data\\add-faq.csv";
            bool result = await program.AddToQnA(addFaq);
            Assert.IsTrue(result);
            int answers = await program.Ask("WHAT DO YOU MEAN BY POINTS? HOW DO I EARN IT?", false, 8);
            result = answers > 0;
            Assert.IsTrue(result);

            return result;
        }
#endif
#if (UpdateEnabled)
        //
        // Update the existing two quesions to remove temporary text. 
        private async Task<bool> UpdateQuestions()
        {
            string updateFaq = "..\\..\\..\\..\\Data\\update-faq.csv";
            bool result = await program.UpdateQnA(updateFaq);
            Assert.IsTrue(result);
            return result;
        }
#endif
#if (PublishEnabled)
        //
        // Update the existing two quesions to remove temporary text. 
        private async Task<bool> PublishKnowledgeBase()
        {
            await program.Publish();
            int answers = await program.Ask("WHAT DO YOU MEAN BY POINTS? HOW DO I EARN IT?", true, 1);
            bool result = answers > 0;
            Assert.IsTrue(result);
            return result;
        }
#endif
#if (TestCreate)
        [TestMethod()]
        public void TestCreate()
        {
            Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
        }     
#endif
#if (TestAsk)
        [TestMethod()]
        public void TestAsk()
        {
            Task.Run(async () => { Assert.IsTrue(await AskQuestion()); }).Wait();
        }
#endif
#if (TestAdd)
        [TestMethod()]
        public void TestAdd()
        {
            Task.Run(async () => { Assert.IsTrue(await AddQuestions()); }).Wait();
            Task.Run(async () => { Assert.IsTrue(await AskQuestion()); }).Wait();
        }     
#endif
#if (TestUpdate)
        [TestMethod()]
        public void TestUpdate()
        {
            Task.Run(async () => { Assert.IsTrue(await UpdateQuestions()); }).Wait();
            Task.Run(async () => { Assert.IsTrue(await AskQuestion()); }).Wait();
        }     
#endif
#if (TestPublish)
        [TestMethod()]
        public void TestPublish()
        {
            Task.Run(async () => { Assert.IsTrue(await PublishKnowledgeBase()); }).Wait();
            Task.Run(async () => { Assert.IsTrue(await AskQuestion(true)); }).Wait();
        }     
#endif
    }
}
