# Fajlol
## Ferramenta de auxílio a jogadores de League of Legends

Requisitos para rodar o projeto:
1. Ter instalado o [Sdk 2.1 do dotnet core](https://dotnet.microsoft.com/download/dotnet-core/2.1)
2. Uma instância do [MongoDB](https://www.mongodb.com/)
3. [Visual Studio 2019](https://visualstudio.microsoft.com/pt-br/vs/) ou [Node.js](https://nodejs.org/en/)

Este projeto é o meu trabalho de conclusão do curso [Tecnologia em Análise e Desenvolvimento de Sistemas do Instituto Federal Catarinense - Campus Blumenau](http://blumenau.ifc.edu.br/tads/).  
A ferramenta tem por objetivo auxiliar o jogador durante a partida para que ele tenha melhores resultados, com foco principalmente nos jogadores iniciantes.  
Para tal foi desenvolvido este projeto Web utilizando ASP.NET Core, Jquery e MongoDB.  
Caso deseje executar este projeto em produção, é recomendado que remova algumas ações que foram criadas somente com propósito de teste ou de executar certas rotinas, sendo elas:  
1. ItensCampeao/CalcularItens
2. PartidaAtual/PreverPartidas
3. PartidaAtual/GravarResultadoPrevisaoPartidas

Para que o projeto funcione, é necessário adicionar a chave da API, basta executar o seguinte comando dentro da pasta do projeto para adicionar um secret:  
```bash
dotnet user-secrets set "ApiConfiguration:Key" "RGAPI-00000000-0000-0000-0000-000000000000"
```
A chave acima é fictícia, para obtê-la é necessário criar uma conta no [portal de desenvolvedores da Riot Games](https://developer.riotgames.com/).  

Também é necessário apontar o projeto para alguma instância do MongoDB no appsettings.json. Atualmente está apontando para localhost na porta padrão em que o MongoDB fica instalado:
```Json
"ConnectionString": "mongodb://localhost:27017",
```

Após isso, basta fazer o download do [Visual Studio 2019](https://visualstudio.microsoft.com/pt-br/vs/) e executar o projeto.

Caso deseje executar pelo dotnet cli você deve ir na pasta TCC e restaurar os pacotes de front-end primeiro executando
```shell
yarn install #ou npm install
```
e depois
```shell
gulp
```
Após isso basta executar o projeto com
```shell
dotnet run
```
