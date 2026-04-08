# Importador de GTIN/EAN Desktop

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WPF-Desktop-0078D4?logo=windows" alt="WPF" />
  <img src="https://img.shields.io/badge/PostgreSQL-Npgsql-4169E1?logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Material%20Design-v5-00BCD4" alt="Material Design" />
  <img src="https://img.shields.io/github/v/release/xp3z41x/ImportadorDeGTINEAN.Desktop?label=vers%C3%A3o" alt="Release" />
</p>

Aplicação desktop Windows para importação em lote de códigos de barras **GTIN/EAN** a partir de planilhas Excel (`.xlsx`) para o banco de dados PostgreSQL do **ERP Integra** da **AtualSistemas**.

> **Importante:** Esta aplicação foi desenvolvida exclusivamente para o banco de dados do ERP **Integra** (AtualSistemas). Ela opera diretamente nas tabelas `cadpro` (cadastro de produtos) e `formar` (cadastro de marcas/fabricantes) do sistema Integra via conexão PostgreSQL.

---

## Índice

- [Visão Geral](#visão-geral)
- [Download](#download)
- [Funcionalidades](#funcionalidades)
- [Capturas de Tela](#capturas-de-tela)
- [Formato da Planilha](#formato-da-planilha)
- [Fluxo de Uso](#fluxo-de-uso)
- [Validação de Códigos de Barras](#validação-de-códigos-de-barras)
- [Match Inteligente de Referências](#match-inteligente-de-referências)
- [Status de Análise](#status-de-análise)
- [Configuração](#configuração)
- [Tabelas do ERP Integra Utilizadas](#tabelas-do-erp-integra-utilizadas)
- [Requisitos do Sistema](#requisitos-do-sistema)
- [Tecnologias](#tecnologias)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Compilação a partir do código-fonte](#compilação-a-partir-do-código-fonte)
- [Licença](#licença)

---

## Visão Geral

O **Importador de GTIN/EAN** resolve o problema de cadastrar manualmente códigos de barras no ERP Integra (AtualSistemas). Frequentemente, fabricantes e fornecedores enviam planilhas Excel contendo as referências dos produtos e seus respectivos códigos EAN/GTIN. Cadastrá-los um a um no sistema é lento e propenso a erros.

Esta ferramenta automatiza todo o processo:

1. **Lê** a planilha Excel do fornecedor
2. **Localiza** cada produto no banco de dados do Integra por match inteligente de referência
3. **Valida** cada código de barras (checkdigit EAN-8, EAN-13, GTIN-14)
4. **Detecta** duplicatas e códigos já cadastrados
5. **Exibe** um preview completo com status colorido para revisão
6. **Atualiza** o banco de dados apenas com os registros selecionados pelo usuário

---

## Download

Baixe o executável pronto para uso na página de [Releases](https://github.com/xp3z41x/ImportadorDeGTINEAN.Desktop/releases/latest):

> **`ImportadorDeGTINEAN.Desktop.exe`** — executável self-contained, não requer instalação do .NET

Basta baixar e executar. Nenhuma dependência externa é necessária.

---

## Funcionalidades

### Importação Inteligente
- Leitura de planilhas Excel `.xlsx` com detecção automática de cabeçalho
- Match inteligente de referências (exato + tokenizado por separadores)
- Validação completa de checkdigit para EAN-8, EAN-13 e GTIN-14
- Detecção de duplicatas no banco de dados e dentro da própria planilha
- Detecção de códigos de barras já cadastrados no produto

### Controle Granular
- Checkboxes individuais para selecionar quais registros atualizar
- Botão "Marcar Todos" e "Desmarcar Todos"
- Filtro por marca/fabricante com seleção em lote
- Confirmação antes de executar a atualização no banco

### Interface Moderna
- Material Design 3 com tema claro
- DataGrid com linhas coloridas por status (verde, laranja, vermelho, etc.)
- Contadores em tempo real por categoria de status
- Log de operações com timestamp
- Barra de progresso durante análise e atualização

### Performance
- Análise 100% em background thread (não trava a interface)
- Apenas 2 queries SQL para carregar todos os dados (referências + barcodes)
- Lookup de duplicatas em memória via HashSet (O(1) por consulta)
- Virtualização do DataGrid para milhares de linhas
- Auto-ajuste de largura das colunas após análise

---

## Formato da Planilha

A planilha Excel (`.xlsx`) deve conter **duas colunas**:

| Coluna A | Coluna B |
|----------|----------|
| Referência do produto | Código de barras (EAN/GTIN) |

### Regras de leitura

- A **primeira aba** da planilha é utilizada
- **Cabeçalhos são detectados automaticamente** — se as primeiras linhas contiverem palavras como "referência", "código", "produto", "EAN", "GTIN" ou "barras", serão ignoradas
- Linhas com a coluna B vazia são ignoradas
- Linhas completamente vazias são ignoradas
- Células numéricas, datas e fórmulas são lidas corretamente (conversão automática para texto)

### Exemplo

| A | B |
|---|---|
| Referência | Código de Barras |
| ABC-123 | 7891234567895 |
| XYZ/456 | 7898765432101 |
| PROD 789 | 78901234 |

---

## Fluxo de Uso

### 1. Configurar Conexão
Clique no ícone de engrenagem (⚙) no canto superior direito para abrir as configurações. Informe os dados de conexão do PostgreSQL do ERP Integra:

- **Servidor** — IP ou hostname do servidor PostgreSQL
- **Porta** — porta do PostgreSQL (padrão: 5432)
- **Banco de Dados** — nome do banco do Integra (ex.: `integrapgsql`)
- **Usuário** — usuário do PostgreSQL
- **Senha** — senha do PostgreSQL

Use o botão **Testar Conexão** para verificar antes de salvar.

### 2. Selecionar Planilha
Clique em **Procurar** (ícone de pasta) e selecione o arquivo `.xlsx` do fornecedor.

### 3. Analisar
Clique em **ANALISAR**. A aplicação irá:
- Ler todas as linhas da planilha
- Buscar todas as referências e códigos de barras do banco do Integra
- Fazer o match de cada referência da planilha com o banco
- Validar cada código de barras
- Verificar duplicatas
- Exibir o resultado no grid com cores e status

### 4. Revisar e Selecionar
Revise os resultados no grid. Cada linha mostra:
- Referência da planilha e do banco
- Descrição e marca do produto (do cadastro do Integra)
- Status e mensagem detalhada

Use os checkboxes para selecionar quais registros deseja atualizar. Utilize os botões de seleção em lote ou o filtro por marca para agilizar.

### 5. Executar Atualização
Clique em **EXECUTAR ATUALIZAÇÃO**. Uma confirmação será exibida com a quantidade de registros selecionados. Após confirmar, o banco de dados do Integra será atualizado.

---

## Validação de Códigos de Barras

A aplicação valida rigorosamente cada código de barras antes de permitir a importação:

### Formatos aceitos

| Formato | Quantidade de dígitos | Exemplo |
|---------|----------------------|---------|
| EAN-8 | 8 dígitos | `78901234` |
| EAN-13 | 13 dígitos | `7891234567895` |
| GTIN-14 | 14 dígitos | `17891234567892` |

### Validação de checkdigit

O dígito verificador é calculado pelo algoritmo padrão GS1 (Módulo 10):

1. Considerar todos os dígitos exceto o último (checkdigit)
2. Da direita para a esquerda, multiplicar alternadamente por **3** e **1**
3. Somar todos os produtos
4. Checkdigit = (10 − soma mod 10) mod 10
5. Comparar com o último dígito informado

### Mensagens de erro

| Situação | Mensagem |
|----------|----------|
| Campo vazio | Código de barras vazio |
| Caracteres não numéricos | Contém caracteres não numéricos |
| Tamanho incorreto | Tamanho inválido (N dígitos). Esperado: 8, 13 ou 14 |
| Checkdigit inválido | Dígito verificador inválido. Esperado: X, encontrado: Y |

---

## Match Inteligente de Referências

A referência na planilha do fornecedor raramente é idêntica à referência cadastrada no ERP Integra. A aplicação utiliza um algoritmo de match inteligente em duas etapas:

### Etapa 1 — Match Exato (após normalização)
1. Remove espaços nas bordas
2. Remove caracteres especiais no início e fim (ex.: `-`, `/`, `.`)
3. Converte para minúsculas
4. Compara a referência normalizada da planilha com a do banco

**Exemplo:** Planilha `"ABC123"` → Banco `" abc123 "` → **Match**

### Etapa 2 — Match por Tokens (se a etapa 1 falhar)
1. A referência do banco é dividida por separadores: `/`, `-`, `\`, espaço
2. Cada token é normalizado e comparado com a referência da planilha

**Exemplos:**

| Planilha | Banco | Match? | Razão |
|----------|-------|--------|-------|
| `ABC123` | `ABC123` | ✅ | Exato |
| `abc123` | `ABC-123-XYZ` | ✅ | Token `ABC-123` normaliza para `abc123` |
| `XY456` | `MARCA/XY456` | ✅ | Token `XY456` corresponde |
| `ABC` | `ABCDEF` | ❌ | Sem match parcial |

---

## Status de Análise

Cada linha da planilha recebe um status após a análise:

| Status | Cor | Significado |
|--------|-----|-------------|
| **OK** | 🟢 Verde | Referência encontrada, barcode válido, pronto para atualizar |
| **Não Encontrado** | 🟠 Laranja | Referência da planilha não encontrada no banco do Integra |
| **EAN Inválido** | 🔴 Vermelho | Código de barras com formato ou checkdigit inválido |
| **Duplicado** | 🟣 Rosa | Código de barras já existe em outro produto no banco |
| **Já Existe** | ⚪ Cinza | O produto já possui este código de barras cadastrado |
| **Atualizado** | 🟢 Verde escuro | Código de barras atualizado com sucesso no banco |
| **Erro** | 🔴 Vermelho escuro | Erro inesperado durante a atualização |

---

## Configuração

As configurações de conexão são salvas no arquivo `App.config` junto ao executável:

| Chave | Descrição | Padrão |
|-------|-----------|--------|
| `PgHost` | Servidor PostgreSQL | `localhost` |
| `PgPort` | Porta | `5432` |
| `PgDatabase` | Nome do banco de dados do Integra | `integrapgsql` |
| `PgUser` | Usuário PostgreSQL | `postgres` |
| `PgPassword` | Senha | (vazio) |

As configurações são editáveis pela tela de configurações da aplicação (ícone ⚙).

---

## Tabelas do ERP Integra Utilizadas

Esta aplicação acessa diretamente o banco de dados PostgreSQL do **ERP Integra** (AtualSistemas). As tabelas utilizadas são:

### `cadpro` — Cadastro de Produtos

| Coluna | Uso na aplicação |
|--------|------------------|
| `referencia` | Chave de busca para match com a planilha |
| `codigo_barra` | Campo atualizado com o novo código EAN/GTIN |
| `descricao` | Exibida no grid para identificação do produto |
| `marca` | ID da marca, usado para JOIN com tabela `formar` |

### `formar` — Cadastro de Marcas/Fabricantes

| Coluna | Uso na aplicação |
|--------|------------------|
| `marca` | Chave de ligação com `cadpro.marca` |
| `descricao` | Nome da marca exibido na coluna "Marca" do grid |

### Consultas SQL executadas

```sql
-- Carrega todas as referências com descrição e marca
SELECT c.referencia, c.codigo_barra, c.descricao, f.descricao AS marca_nome
FROM cadpro c
LEFT JOIN formar f ON c.marca = f.marca
WHERE c.referencia IS NOT NULL

-- Carrega todos os códigos de barras existentes (para detecção de duplicatas)
SELECT codigo_barra FROM cadpro
WHERE codigo_barra IS NOT NULL AND codigo_barra <> ''

-- Atualiza o código de barras de um produto
UPDATE cadpro SET codigo_barra = @barcode WHERE referencia = @ref
```

> **Nota:** A aplicação utiliza queries parametrizadas para prevenir SQL injection. Nenhuma operação destrutiva (DELETE, DROP, ALTER) é executada.

---

## Requisitos do Sistema

| Requisito | Detalhe |
|-----------|---------|
| **Sistema Operacional** | Windows 10 ou 11 (x64) |
| **ERP** | Integra (AtualSistemas) com banco PostgreSQL acessível na rede |
| **Rede** | Acesso ao servidor PostgreSQL do Integra |
| **.NET** | Não requer instalação (executável self-contained) |

---

## Tecnologias

| Tecnologia | Versão | Finalidade |
|------------|--------|------------|
| .NET | 8.0 | Framework principal |
| WPF | — | Interface gráfica desktop |
| Material Design Themes | 5.1.0 | UI moderna com Material Design 3 |
| Material Design Colors | 3.1.0 | Paleta de cores MD |
| ClosedXML | 0.104.2 | Leitura de planilhas Excel (.xlsx) |
| Npgsql | 8.0.6 | Driver PostgreSQL para .NET |
| System.Configuration.ConfigurationManager | 8.0.1 | Gerenciamento de App.config |

---

## Estrutura do Projeto

```
ImportadorDeGTINEAN.Desktop/
├── ImportadorDeGTINEAN.Desktop.sln    # Solution
├── ImportadorDeGTINEAN.Desktop.csproj # Projeto
├── App.config                         # Configurações de conexão
├── App.xaml / App.xaml.cs             # Entry point e tema Material Design
│
├── Models/
│   ├── ImportStatus.cs                # Enum de status (Matched, NoMatch, etc.)
│   ├── ExcelRow.cs                    # Dados de uma linha da planilha
│   └── AnalysisResult.cs             # Resultado da análise (item do DataGrid)
│
├── Services/
│   ├── ExcelReaderService.cs          # Leitura de planilhas .xlsx
│   ├── ReferenceMatcherService.cs     # Match inteligente de referências
│   ├── BarcodeValidatorService.cs     # Validação EAN-8/13, GTIN-14
│   ├── DatabaseService.cs            # Operações PostgreSQL (Integra)
│   └── SettingsService.cs            # Leitura/escrita do App.config
│
├── ViewModels/
│   ├── BaseViewModel.cs              # INotifyPropertyChanged + RelayCommand
│   ├── MainViewModel.cs              # Orquestração de análise e atualização
│   └── SettingsViewModel.cs          # Configuração de conexão PostgreSQL
│
├── Views/
│   ├── MainWindow.xaml / .xaml.cs     # Tela principal
│   └── SettingsWindow.xaml / .xaml.cs # Tela de configurações
│
└── Converters/
    └── StatusToColorConverter.cs      # Conversores de status para cores
```

---

## Compilação a partir do código-fonte

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```bash
dotnet build -c Release
```

### Publicar executável self-contained

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true
```

O executável será gerado em:
```
bin/Release/net8.0-windows/win-x64/publish/ImportadorDeGTINEAN.Desktop.exe
```

---

## Licença

Este projeto é de uso interno para integração com o ERP Integra (AtualSistemas).
