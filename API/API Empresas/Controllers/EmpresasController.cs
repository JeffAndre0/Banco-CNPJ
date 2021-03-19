using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API_Empresas.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmpresaController : ControllerBase
    {
        Data.MongoDB _mongoDB;
        IMongoCollection<BsonDocument> colecao_empresas;

        public EmpresaController(Data.MongoDB mongoDB)
        {
            _mongoDB = mongoDB;
            colecao_empresas = _mongoDB.DB.GetCollection<BsonDocument>("Empresa");
        }

        [HttpGet]
        public IEnumerable<BsonDocument> ObterEmpresas()
        {
            return colecao_empresas.Find(_ => true).ToList();
        }

        [HttpGet("{CNPJ}")]
        public IEnumerable<BsonDocument> Get(string cnpj)
        {

            var filtro = new BsonDocument { { "CNPJ", cnpj } };
            return colecao_empresas.Find(filtro).ToList();
        }

    }
}
