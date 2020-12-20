using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.VisualBasic.FileIO;

namespace QnA
{
    public class QnAService
    {
        /// <summary>
        /// Constructor loads "appsettings.json" (see: Config)
        /// </summary>
        public QnAService()
        {
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            queryEndpoint = $"https://{config["ApplicationName"]}.azurewebsites.net";
            queryEndpointKey = config["QueryEndpointKey"];
            if (queryEndpointKey != null && queryEndpointKey.Length == 0)
            {
                queryEndpointKey = null;
            }
            knowledgeBaseID = config["KnowledgeBaseID"];
            if (knowledgeBaseID != null && knowledgeBaseID.Length == 0)
            {
                knowledgeBaseID = null;
            }
        }

        /// <summary>
        /// Configuration, use appsettings.json to retain the Knowledge Base ID written out when QnA knowledge base is created and the endpoint key for knowledge base queries.
        /// Contains:
        ///   See azure portal, under resource used for QnA maker
        ///     AuthoringKey: from azure cognitive service
        ///     ServiceName: cognitive service name 
        ///     ApplicationName: app service name 
        ///  Written out when QnA created
        ///    KnowledgeBaseId: NOTE: Ater creation it can only be found in from www.qnamaker.ai the knowledge base has been published.
        ///    QueryEndpointKey: Authorization key for asking questions and training.
        /// </summary>
        public IConfiguration Config { get { return config; } }

        /// <summary>
        /// Id uniquely identifies the Knowledge base used.
        /// </summary>
        public string KnowledgeBasedID { get { return knowledgeBaseID; } set { knowledgeBaseID = value; } }
        /// <summary>
        /// Authorization for the QnA site (the same for all knowledge bases)
        /// </summary>
        public string QueryEndpointKey { get { return queryEndpointKey; } set { queryEndpointKey = value; } }
        /// <summary>
        /// Create a new QnA Knowledge base, the returned ID and endpoint key should be placed
        /// in the appsettings.json file or they must be set manually (see properties: KnowledgeBaseID
        /// and QueryEndpointKey) when a QnAService object is instantiated.
        /// </summary>
        /// <param name="qnaData">Creation data.</param>
        /// <returns>( status,
        ///            knowledgeBaseId,
        ///            queryEndPoint )</returns>
        public async Task<(string, string, string)> CreateQnA(CreateKbDTO qnaData)
        {
            Operation op = await Authoring.Knowledgebase.CreateAsync(qnaData);
            op = await MonitorOperation(op);
            string result = op.ResourceLocation;
            if (op.OperationState == OperationStateType.Succeeded)
            {
                knowledgeBaseID = op.ResourceLocation.Replace("/knowledgebases/", string.Empty);
                string endpointKey = await GetQueryKey();
                return (op.OperationState, knowledgeBaseID, endpointKey);
            }
            else
            {
                return (op.OperationState, null, null);
            }
        }
        /// <summary>
        /// Add new entries to the knowledge base, if the answer already exists appends the new questions to the existing list.
        /// </summary>
        /// <param name="entries">Answer and a list of questions to add.</param>
        /// <param name="published">True - work on the published knowledge base, false - test knowledge base.</param>
        /// <returns>Status, Error</returns>
        public async Task<(string, string)> AddToQnA(IList<QnADTO> entries, bool published)
        {
            return await ModifyQnA(entries, published, false);
        }
        /// <summary>
        /// Add new entries to the knowledge base, if the answer already exists replaces the existing questions with the new list.
        /// </summary>
        /// <param name="entries">Answer and a list of questions to update</param>
        /// <param name="published">True - work on the published knowledge base, false - test knowledge base.</param>
        /// <returns></returns>
        public async Task<(string, string)> UpdateQnA(IList<QnADTO> entries, bool published)
        {
            return await ModifyQnA(entries, published, true);
        }
        /// <summary>
        /// Train the knowledge base with a list of answers and corresponding questions. For every question it finds
        /// the current answers and if one matches the supplied answer it is trained for that question.
        /// </summary>
        /// <param name="entries">Answer and a list of questions to train.</param>
        /// <param name="published">True - work on the published knowledge base, false - test knowledge base.</param>
        /// <returns></returns>
        public async Task Train(IList<QnADTO> entries, bool published)
        {
            List<FeedbackRecordDTO> feedback = new List<FeedbackRecordDTO>();
            foreach (QnADTO entry in entries)
            {
                foreach (string question in entry.Questions)
                {
                    QnASearchResultList answers = await Ask(question, published, 8);
                    foreach (QnASearchResult answer in answers.Answers)
                    {
                        if (String.Compare(answer.Answer, entry.Answer) == 0)
                        {
                            FeedbackRecordDTO update = new FeedbackRecordDTO { UserId = "QnAService", QnaId = answer.Id, UserQuestion = question };
                            feedback.Add(update);
                        }
                    }
                }
            }
            if (feedback.Count > 0)
            {
                QnAMakerRuntimeClient client = await Client();
                FeedbackRecordsDTO records = new FeedbackRecordsDTO { FeedbackRecords = feedback };
                await client.Runtime.TrainAsync(knowledgeBaseID, records);
            }
        }
        /// <summary>
        /// Ask the knowledge base a question.
        /// </summary>
        /// <param name="question">Question to ask</param>
        /// <param name="published">True - work on the published knowledge base, false - test knowledge base.</param>
        /// <param name="topQuestions">The maximum number of answers to return.</param>
        /// <returns></returns>
        public async Task<QnASearchResultList> Ask(string question, bool published, int topQuestions = 1)
        {
            QnAMakerRuntimeClient client = await Client();
            QueryDTO query = new QueryDTO { Question = question, Top = topQuestions, IsTest = !published };
            QnASearchResultList response = await client.Runtime.GenerateAnswerAsync(knowledgeBaseID, query);
            return response;
        }
        /// <summary>
        /// Delete the knowledge base
        /// </summary>
        /// <returns></returns>
        public async Task DeleteKnowledgeBase()
        {
            await Authoring.Knowledgebase.DeleteAsync(knowledgeBaseID);
        }
        /// <summary>
        /// Delete the knowledge base.
        /// </summary>
        public async Task Publish()
        {
            await Authoring.Knowledgebase.PublishAsync(knowledgeBaseID);
        }
        /// <summary>
        /// Load answer and questions from a CSV formated file. The file expected to have 
        /// "Answer", "question 1", "question 2", ...
        /// 
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <summary>
        /// Load answer and questions from a CSV formated file. The file format must be:
        /// "Answer", "question 1", "question 2", ...
        /// 
        /// Embedded double quotes should be delimited with another double quote [i.e., "This how to embed ""double quotes"" in an answer or question"]
        /// </summary>
        /// <param name="file">path to csv file.</param>
        /// <returns>List of QnADTO objects, one for each line.</returns>
        /// 
        public List<QnADTO> LoadCSV(string file)
        {
            // Expected format
            // <optional id> <answer> <question> <question> ...
            List<QnADTO> answers = new List<QnADTO>();

            using (TextFieldParser parser = new TextFieldParser(file))
            {
                string[] row;                // 
                                             // Separate rows based on comma.
                parser.Delimiters = new string[] { "," };
                parser.TrimWhiteSpace = true;
                //
                // Ensure the format is correct
                if (parser.EndOfData)
                {
                    // No data
                    throw new Exception("Question and Answer file is empty.");
                }

                while (parser.EndOfData == false)
                {
                    try
                    {
                        row = parser.ReadFields();
                        if (row.Length < 2)
                        {
                            throw new ArgumentException("Missing column.");
                        }
                        //
                        // New Answer, get the key 
                        QnADTO entry = new QnADTO();
                        int index = 0;
                        entry.Answer = row[index++];
                        entry.Questions = new List<String>();
                        for (; index < row.Length; index++)
                        {
                            entry.Questions.Add(row[index]);
                        }
                        answers.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Skipping line ${parser.LineNumber}, ${ex}");
                    }
                }
            }
            return answers;
        }

        // ==============
        // Private fields and functions
        string queryEndpoint;
        string queryEndpointKey;
        string knowledgeBaseID;

        /// <summary>
        /// Authoring used for creating and deleting knowledge bases. It also adds, updates and delete entries.
        /// </summary>
        QnAMakerClient authoring;

        /// <summary>
        /// Client used to ask questions and train knowledge base.
        /// </summary>
        QnAMakerRuntimeClient client;

        /// <summary>
        /// Contains values from appsettings.json
        /// </summary>
        IConfiguration config;

        /// <summary>
        /// Lazy loading of the authoriing client (crud operations)
        /// </summary>
        private QnAMakerClient Authoring { get { return authoring ?? CreateAuthoringClient(); } }
        private QnAMakerClient CreateAuthoringClient()
        {
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(config["AuthoringKey"]);
            authoring = new QnAMakerClient(credentials);
            authoring.Endpoint = $"https://{config["ResourceName"]}.cognitiveservices.azure.com";
            return authoring;
        }
        /// <summary>
        /// Lazy loading of the runtime client (query, training)
        /// </summary>
        /// <returns></returns>
        private async Task<QnAMakerRuntimeClient> Client()
        {
            return client ?? await CreateRuntimeClient();
        }
        private async Task<QnAMakerRuntimeClient> CreateRuntimeClient()
        {
            var endpointKey = await GetQueryKey();
            var credentials = new EndpointKeyServiceClientCredentials(endpointKey);
            client = new QnAMakerRuntimeClient(credentials) { RuntimeEndpoint = queryEndpoint };
            return client;
        }
        private async Task<string> GetQueryKey()
        {
            return queryEndpointKey ?? await CreateEndpointKey();
        }
        /// <summary>
        /// If Query enpoint is not set download from the QnA maker cognitive service.
        /// </summary>
        /// <returns></returns>
        private async Task<string> CreateEndpointKey()
        {
            var endpointKeysObject = await Authoring.EndpointKeys.GetKeysAsync();
            queryEndpointKey = endpointKeysObject.PrimaryEndpointKey;
            return queryEndpointKey;
        }
        /// <summary>
        /// Download all the complete knowledge base and store based on each answer.
        /// </summary>
        /// <param name="published"></param>
        /// <returns>Dictionary of entire knowledge base</returns>
        private async Task<Dictionary<string, QnADTO>> GetExistingAnswers(bool published)
        {
            QnADocumentsDTO kb = await Authoring.Knowledgebase.DownloadAsync(knowledgeBaseID, published ? EnvironmentType.Prod : EnvironmentType.Test);
            Dictionary<string, QnADTO> existing = new Dictionary<string, QnADTO>();
            foreach (QnADTO entry in kb.QnaDocuments)
            {
                // Does not handle duplicates.
                existing.Add(entry.Answer, entry);
            }
            return existing;
        }
        /// <summary>
        /// Adjust all then entries in the knowledge base based on the values passed in. Assumes that there are no duplicate answers in the data base.
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="published"></param>
        /// <param name="removeExisting"></param>
        /// <returns>Status, Error</returns>
        private async Task<(string, string)> ModifyQnA(IList<QnADTO> entries, bool published, bool removeExisting)
        {
            Dictionary<string, QnADTO> existing = await GetExistingAnswers(published);

            List<UpdateQnaDTO> updates = new List<UpdateQnaDTO>();
            List<QnADTO> additions = new List<QnADTO>();
            if (entries != null && entries.Count > 0)
            {
                foreach (QnADTO update in entries)
                {
                    QnADTO value = null;
                    if (existing.TryGetValue(update.Answer, out value))
                    {
                        UpdateQnaDTO modified = new UpdateQnaDTO();
                        modified.Answer = value.Answer;
                        modified.Id = value.Id;
                        modified.Questions = new UpdateQnaDTOQuestions(update.Questions, removeExisting ? value.Questions : null);
                        updates.Add(modified);
                    }
                    else
                    {
                        additions.Add(update);
                    }
                }
            }
            return await AlterKb(additions, updates, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="additions"></param>
        /// <param name="updates"></param>
        /// <param name="deletes"></param>
        /// <returns></returns>
        private async Task<(string, string)> AlterKb(IList<QnADTO> additions, IList<UpdateQnaDTO> updates, IList<Nullable<Int32>> deletes)
        {
            var update = new UpdateKbOperationDTO
            {
                Add = additions != null && additions.Count > 0 ? new UpdateKbOperationDTOAdd { QnaList = additions } : null,
                Update = updates != null && updates.Count > 0 ? new UpdateKbOperationDTOUpdate { QnaList = updates } : null,
                Delete = deletes != null && deletes.Count > 0 ? new UpdateKbOperationDTODelete { Ids = deletes } : null
            };
            var op = await Authoring.Knowledgebase.UpdateAsync(knowledgeBaseID, update);
            op = await MonitorOperation(op);
            return (op.OperationState, op.ErrorResponse == null ? null : op.ErrorResponse.ToString());
        }
        /// <summary>
        /// Wait for the operation to complete, knowledge base not updated until completed.
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        internal async Task<Operation> MonitorOperation(Operation operation)
        {
            // Loop while operation is success
            for (int i = 0;
                i < 20 && (operation.OperationState == OperationStateType.NotStarted || operation.OperationState == OperationStateType.Running);
                i++)
            {
                Console.WriteLine("Waiting for operation: {0} to complete.", operation.OperationId);
                await Task.Delay(5000);
                operation = await Authoring.Operations.GetDetailsAsync(operation.OperationId);
            }

            if (operation.OperationState != OperationStateType.Succeeded)
            {
                throw new Exception($"Operation {operation.OperationId} failed to completed.");
            }
            return operation;
        }
    }

}
