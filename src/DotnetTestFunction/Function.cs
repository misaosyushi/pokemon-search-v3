using System;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DotnetTestFunction
{
    public class Function
    {
        
        // private static readonly AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        // private static readonly string tableName = "PokemonTable";
        
        // TODO: requestの文字列取る方法を調べる
        public async Task<string> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine("====== start lambda =======");
            Console.WriteLine(request.Body); // ここには入ってないっぽい

            var client = new AmazonDynamoDBClient();
            var ctx = new DynamoDBContext(client);
            var items = await ctx.ScanAsync<PokemonTableItem>(null).GetRemainingAsync();
            
            items.ForEach(item => Console.WriteLine(item.Id));

            return "success";
        }
    }
    
    [DynamoDBTable("PokemonTable")]
    class PokemonTableItem
    {
        [DynamoDBHashKey("id")]
        public string Id { get; set; }

        [DynamoDBProperty("types")]
        public string[] Types { get; set; }
    }
}
