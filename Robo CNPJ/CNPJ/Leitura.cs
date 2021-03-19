using System;
using System.IO;
using System.Net;
using System.Text;
using System.IO.Compression;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Teste_F360
{
    class Leitura
    {

        static readonly string conexao = "STRING DE CONEXAO"; //Inserir a string de conexao do Mongodb Aqui
        static readonly string db_Name = "CNPJEmpresas";
        static readonly string caminho_base = AppDomain.CurrentDomain.BaseDirectory + @"\Base de Dados\";
        static readonly string table_socios = "Socios";
        static readonly string table_empresas = "Empresas";
        static readonly string url = "https://receita.economia.gov.br/orientacao/tributaria/cadastros/cadastro-nacional-de-pessoas-juridicas-cnpj/dados-publicos-cnpj";
        static List<string> sites = new List<string>();
        static readonly string caminho_zip = AppDomain.CurrentDomain.BaseDirectory + @"\Downloads\";
        //Limitar o número de itens ao banco de dados?
        static bool limite = true;
        //Limite de entradas ao banco de dados (apenas para não ultrapassar o limite do banco - para testes)
        static int contador = 1000;
        static async Task Main(string[] args)
        {

            Console.WriteLine("Escolha o que deseja Fazer");
            Console.WriteLine("Opção 1...Baixar e extrair arquivos de CNPJ do site da Receita Federal");
            Console.WriteLine("Opção 2...Enviar CNPJs ao banco de dados");
            Console.WriteLine("Opção 3...Efetuar ambas as operações");
            Console.WriteLine("");

            string resp = Console.ReadLine();
            if (resp == "1" || resp == "3")
            {
                Console.WriteLine("Opção Baixar e extrair arquivos de CNPJ");
                await BuscaLink(url);
                Extrair(caminho_zip, caminho_base);
            }
            if (resp == "2" || resp == "3")
            {
                await Upload_DBAsync(caminho_base);
            }
        }

        /// <summary>
        /// Upload dos arquivos ao banco de dados
        /// </summary>
        /// <param name="dir_base">Diretorio dos arquivos de CNPJ</param>
        /// <returns></returns>
        static public async Task Upload_DBAsync(string dir_base)
        {
            List<string> lista_arquivos = new List<string>(Directory.GetFiles(dir_base));

            var dbclient = ConectarBanco();
            if (dbclient == null)
            {
                Console.WriteLine("Não foi possível se conectar ao banco de dados.\nVerifique se as informações estão corretas\nVerifique a variável 'conexao' e se o seu IP está habilidado para leitura ao cluster");
                return;
            }
            var BancoDados = dbclient.GetDatabase(db_Name);

            var socios = BancoDados.GetCollection<BsonDocument>(table_socios);
            var empresas = BancoDados.GetCollection<BsonDocument>(table_empresas);
            long cont_socios = 0;
            long cont_empresas = 0;

            foreach (string arq in lista_arquivos)
            {
                StreamReader arquivo = new StreamReader(arq);

                string identificador;
                string linha;
                Console.WriteLine(arq);
                arquivo.ReadLine();
                try
                {
                    while ((linha = arquivo.ReadLine()) != null)
                    {
                        identificador = linha.Substring(0, 1);
                        if (limite)
                            if (contador <= 0)
                                break;

                        //Empresa
                        if (identificador == "1")
                        {
                            if (limite)
                                contador--;
                            cont_empresas++;
                            var empresa = new BsonDocument
                            {
                                { "CNPJ", linha.Substring(3, 14) },
                                { "IdentificadorMF",  int.Parse(linha.Substring(17, 1))},
                                { "RazaoSocial",  linha.Substring(18, 150).Trim()},
                                { "NomeFantasia", linha.Substring(168, 55).Trim()},
                                { "CapitalSOcial",  linha.Substring(892, 14).Trim()},
                                { "situacaoCadastral",linha.Substring(223, 2).Trim()},
                                { "dataSituacao",DateTime.ParseExact(linha.Substring(225, 8).Trim(), "yyyymmdd", System.Globalization.CultureInfo.InvariantCulture).ToLocalTime().Date},
                                { "cep",int.Parse(linha.Substring(674, 8).Trim())}
                            };

                            try
                            {
                                await empresas.InsertOneAsync(empresa);
                            }
                            catch
                            {
                                Console.WriteLine("Não foi possivel adicinar item ao banco de dados");
                            }
                        }

                        //Sócio
                        else if (identificador == "2")
                        {
                            if (limite)
                                contador--;
                            cont_socios++;
                            var socio = new BsonDocument
                            {
                                { "identificador",int.Parse(linha.Substring(17, 1))},
                                { "nomeSocio", linha.Substring(18, 150).Trim()},
                                { "cnpj", linha.Substring(3, 14) }
                            };

                            try
                            {
                                await socios.InsertOneAsync(socio);
                            }
                            catch
                            {
                                Console.WriteLine("Não foi possivel adicinar item ao banco de dados");
                            }
                        }
                        else
                            continue;
                    }
                }
                catch
                {
                    Console.WriteLine("Erro no looping");
                }
                finally
                {
                    Console.WriteLine("Fim do Arquivo");
                    arquivo.Close();
                }
                Console.WriteLine("Número de empresas Adicionados: " + cont_empresas.ToString());
                Console.WriteLine("Número de sócios Adicionados: " + cont_socios.ToString());
            }
        }

        /// <summary>
        /// Procura os arquivos de CNPJ para downloads no site da receita
        /// </summary>
        static public async Task BuscaLink(string urlAddress)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //Verifica se conseguiu acessar o site corretamente
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }

                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                //Leitura linha a linha do site
                string linha_web = readStream.ReadLine();

                //Busca a primeira linha que contenha o link para download do arquivo zip
                while (!linha_web.Contains(".zip"))
                {
                    if (linha_web == null)
                        break;
                    linha_web = readStream.ReadLine();
                }

                //busca todos os links de download
                while (linha_web.Contains(".zip"))
                {
                    //index para montar link de donwload
                    int index_h = linha_web.IndexOf("href=") + 6;
                    int index_fim = linha_web.IndexOf(".zip") + 4;

                    string link = linha_web[index_h..index_fim];

                    //para testes... download arq versao drive
                    //link = "https://doc-04-0k-docs.googleusercontent.com/docs/securesc/89n0jhpojnt1q330c9ngisenc3diubtd/jgfukmeqj0mu4vlf9r1bc1o43ag75ik9/1615989300000/05148873035656089420/04538634437072923502/11JEE8WKSD9_FBAfGfiFq_z-ZtS1bmGeR?e=download&authuser=0&nonce=i31k23m58oo36&user=04538634437072923502&hash=9802be1cr3jgitkn559ahqf56v6256vg";
                    sites.Add(link);
                    //await Download(link);

                    //Ler proximo link
                    linha_web = readStream.ReadLine();
                }
                Console.WriteLine();

                response.Close();
                readStream.Close();
                foreach (string site in sites)
                {
                    Console.WriteLine(site);
                    await Download(site);
                }
            }
        }

        /// <summary>
        /// Baixa os arquivos CNPJ do site da Receita
        /// </summary>
        /// <param name="url">Link do site da receita que contem os arquivos</param>
        /// <returns></returns>
        static public async Task Download(string url)
        {
            if (!Directory.Exists(caminho_zip))
                Directory.CreateDirectory(caminho_zip);
            if (!Directory.Exists(caminho_base))
                Directory.CreateDirectory(caminho_base);

            string arquivo = url.Substring(url.LastIndexOf('/') + 1);
            Console.WriteLine(caminho_zip + arquivo);

            try
            {
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(url, caminho_zip + arquivo);
                }
                Console.WriteLine("Baixado");

                //client.DownloadFile(url, @"C: \Users\Jeff\source\repos\Teste CNPJ\Teste F360\Base Robo\" + arquivo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error ao baixar", e.Message);
                Console.WriteLine(url);
            }

        }

        /// <summary>
        /// Extrair arquivos zip
        /// </summary>
        /// <param name="zip">Pasta com os arquivos compactados (zip)</param>
        /// <param name="dest">Pasta para armazenar os arquivos descompactados</param>
        static public void Extrair(string zip, string dest)
        {

            List<string> lista_arquivos = new List<string>(Directory.GetFiles(zip));
            foreach (string item in lista_arquivos)
            {
                Console.WriteLine(item);
                if (item.EndsWith(".zip"))
                {
                    ZipFile.ExtractToDirectory(item, dest);
                }
            }
        }

        /// <summary>
        /// Conexão ao Banco de Dados
        /// </summary>
        /// <returns></returns>
        static public MongoClient ConectarBanco()
        {
            string conect;
            //Verifica se foi adicionada a string de conexão
            if (conexao == "STRING DE CONEXAO")
            {
                Console.WriteLine("String de conexão Inválida.\nDigite ela abaixo ou modifique a variável 'conexao' para continuar");
                conect = Console.ReadLine();
            }
            else
                conect = conexao;


            MongoClient dbClient;
            try
            {
                dbClient = new MongoClient(conect);
                var BancoDados = dbClient.GetDatabase(db_Name);
                var socios = BancoDados.GetCollection<BsonDocument>(table_socios);
                var empresas = BancoDados.GetCollection<BsonDocument>(table_empresas);
            }
            catch
            {
                Console.WriteLine("Não foi possível se conectar ao banco de dados, verifique se as informações de conexão estão corretas");
                return null;
            }
            return dbClient;
        }
    }
}
