using System;

namespace API_Empresas.Data.Collections
{
    public class Empresas
    {

        public string CNPJ { get; set; }
        public int Identificador_M_F { get; set; }
        public string Razao_Social { get; set; }
        public string Nome_Fantasia { get; set; }
        public string Capital_Social { get; set; }
        public string Situacao_Cadastral { get; set; }
        public DateTime Data_situacao { get; set; }
        public int CEP { get; set; }
    }
}
