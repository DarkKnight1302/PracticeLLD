using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;
using User = PracticeLLD.Entities.User;

namespace PracticeLLD.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ICosmosDbService cosmosDbService;
        private readonly ILogger<UserRepository> logger;

        public UserRepository(ICosmosDbService cosmosDbService, ILogger<UserRepository> logger)
        {
            this.cosmosDbService = cosmosDbService;
            this.logger = logger;
        }

        public async Task<User> CreateUser(User user)
        {
            var container = GetContainer();
            user.updatedAt = DateTimeOffset.UtcNow;
            user.createdAt = DateTimeOffset.UtcNow;

            ItemResponse<User> resp = await container.CreateItemAsync<User>(user, new PartitionKey(user.id));
            return resp.Resource;
        }

        public async Task<User> CreateUser(string id, string name, bool isGoogleLogin, string email)
        {
            var container = GetContainer();
            User user = new User()
            {
                id = id,
                name = name,
                email = email,
                googleLogin = isGoogleLogin,
                updatedAt = DateTimeOffset.UtcNow,
                createdAt = DateTimeOffset.UtcNow
            };
            ItemResponse<User> resp = await container.CreateItemAsync<User>(user, new PartitionKey(id));
            return resp.Resource;
        }

        public async Task<User> GetUser(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse = await container.ReadItemAsync<User>(userId, new PartitionKey(userId));
                return itemResponse.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<User> GetUserByEmail(string email)
        {
            var container = GetContainer();
            try
            {
                var query = "SELECT * FROM c WHERE c.email = @email";
                var queryDefinition = new QueryDefinition(query).WithParameter("@email", email);

                using var iterator = container.GetItemQueryIterator<User>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var user = response.FirstOrDefault();
                    if (user != null)
                    {
                        return user;
                    }
                }
                return null;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<User> UpdateUser(User user)
        {
            var container = GetContainer();
            user.updatedAt = DateTimeOffset.UtcNow;

            var response = await container.UpsertItemAsync(user, new PartitionKey(user.id));
            return response.Resource;
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("User");
        }
    }
}
