using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API_Empresas.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SociosController : ControllerBase
    {

        Data.MongoDB _mongoDB;
        IMongoCollection<BsonDocument> colecao_socios;

        public SociosController(Data.MongoDB mongoDB)
        {
            _mongoDB = mongoDB;
            colecao_socios = _mongoDB.DB.GetCollection<BsonDocument>("Socio");
        }

        [HttpGet]
        public IEnumerable<BsonDocument> ObterSocios()
        {
            return colecao_socios.Find(_ => true).ToList();
        }

        [HttpGet("{CNPJ}")]
        public IEnumerable<BsonDocument> Get(string cnpj)
        {

            var filtro = new BsonDocument { { "CNPJ", cnpj } };
            return colecao_socios.Find(filtro).ToList();
        }
    }
}
