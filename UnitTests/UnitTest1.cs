using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;

namespace QnA.Tests
{
    [TestClass()]
    public class UnitTest1
    {
        public static QnA.Program program;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            program = new QnA.Program();
        }
        //
        // Create QnA knowledge base.
        // Will contain one Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> CreateDatabase()
        {
            bool success = await program.CreateQnA("..\\..\\..\\..\\Data\\create-faq.csv");

            Assert.IsTrue(success);
            Assert.IsNotNull(program.Service.KnowledgeBasedID);

            // Question has unwanted text in it. Check for the unwanted text.
            QnASearchResultList answer = await program.Service.Ask("HOW CAN I CHANGE MY SHIPPING ADDRESS", false);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.AreEqual(1, answer.Answers.Count);
            Assert.IsNotNull(answer.Answers[0].Questions);
            Assert.AreEqual(1, answer.Answers[0].Questions.Count);
            bool found = answer.Answers[0].Questions[0].Contains("##REPLACE##");
            return found;
        }
        //
        // Add an additional Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> AddQuestion()
        {
            bool success = await program.AddToQnA("..\\..\\..\\..\\Data\\add-faq.csv", false);
            Assert.IsTrue(success);

            // Question has unwanted text in it. Check for the unwanted text. Also question should
            // only return 1 match. The other Answer added should never match this question.
            QnASearchResultList answer = await program.Service.Ask("WHAT DO YOU MEAN BY POINTS", false);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.AreEqual(1, answer.Answers.Count);
            Assert.IsNotNull(answer.Answers[0].Questions);
            Assert.AreEqual(1, answer.Answers[0].Questions.Count);
            bool found = answer.Answers[0].Questions[0].Contains("##REPLACE##");
            return found;
        }
        //
        // Update the existing two quesions to remove temporary text. 
        private async Task<bool> UpdateQuestions()
        {
            bool success = await program.UpdateQnA("..\\..\\..\\..\\Data\\update-faq.csv", false);
            Assert.IsTrue(success);

            // Bad text in question is removed.
            QnASearchResultList answer = await program.Service.Ask("WHAT DO YOU MEAN BY POINTS", false);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.AreEqual(1, answer.Answers.Count);
            Assert.IsNotNull(answer.Answers[0].Questions);
            Assert.AreEqual(1, answer.Answers[0].Questions.Count);
            bool question1 = answer.Answers[0].Questions[0].Contains("##REPLACE##") == false;

            // Bad text in question is removed.
            answer = await program.Service.Ask("HOW CAN I CHANGE MY SHIPPING ADDRESS", false);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.AreEqual(1, answer.Answers.Count);
            Assert.IsNotNull(answer.Answers[0].Questions);
            Assert.AreEqual(1, answer.Answers[0].Questions.Count);
            bool question2 = answer.Answers[0].Questions[0].Contains("##REPLACE##") == false;

            return question1 && question2;
        }
        //
        // Add full text, will add additional questions to the first two entries.
        private async Task<bool> AddFullText()
        {
            bool success = await program.AddToQnA("..\\..\\..\\..\\Data\\full-faq.csv", false);
            Assert.IsTrue(success);

            // Question has unwanted text in it. Check for the unwanted text. Also question should
            // only return 1 match. The other Answer added should never match this question.
            QnASearchResultList answer = await program.Service.Ask("What perks do I get for shopping with you", false);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.AreEqual(1, answer.Answers.Count);
            Assert.IsNotNull(answer.Answers[0].Questions);
            Assert.AreEqual(2, answer.Answers[0].Questions.Count);
            bool found = answer.Answers[0].Answer.Contains("Because you are important to us");
            return found;
        }
        private async Task<bool> TrainQnA()
        {
            string sampleQuestion = "This present is for my mother, how do I get it to her?";
            QnASearchResultList answer = await program.Service.Ask(sampleQuestion, false);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.IsNotNull(answer.Answers[0].Score);
            double score1 = answer.Answers[0].Score ?? 0.0;
            string answer1 = answer.Answers[0].Answer;

            await program.Train("..\\..\\..\\..\\Data\\training-faq.csv", false);
            answer = await program.Service.Ask(sampleQuestion, false);

            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.IsNotNull(answer.Answers[0].Score);
            double score2 = answer.Answers[0].Score ?? 0.0;
            string answer2 = answer.Answers[0].Answer;

            return (String.Compare(answer1, answer2) != 0) || (score2 > score1);
        }
        private async Task<bool> CleanUp()
        {
            await program.DeleteKnowledgeBase();
            return true;
        }

        [TestMethod()]
        public void CrudTest()
        {
            try
            {
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AddQuestion()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await UpdateQuestions()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await AddFullText()); }).Wait();
                //Task.Run(async () => { Assert.IsTrue(await TrainQnA()); }).Wait();
            }
            finally
            {
                if (program.Service.KnowledgeBasedID != null && program.Service.KnowledgeBasedID.Length > 0)
                {
                    Task.Run(async () => { Assert.IsTrue(await CleanUp()); }).Wait();
                }
            }
        }
    }
}
