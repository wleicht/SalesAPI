# ?? Estrutura Organizacional do SalesAPI

## ?? Objetivos da Reorganiza��o

Esta reorganiza��o foi implementada para:

? **Limpar a pasta raiz** - Mover arquivos Docker e scripts para pastas dedicadas  
? **Melhorar a organiza��o** - Agrupar arquivos relacionados logicamente  
? **Facilitar a manuten��o** - Localiza��o intuitiva de configura��es  
? **Padronizar estrutura** - Seguir boas pr�ticas de projetos Enterprise  
? **Simplificar navega��o** - Estrutura mais clara para novos desenvolvedores  

## ?? Nova Estrutura de Pastas

```
SalesAPI/
??? ?? docker/                          # ?? Tudo relacionado ao Docker
?   ??? ?? compose/                     #    Arquivos Docker Compose
?   ?   ??? docker-compose.yml          #    ? Servi�os principais
?   ?   ??? docker-compose-observability-simple.yml # ? Com monitoramento
?   ??? ?? observability/               #    Configura��es de monitoramento
?   ?   ??? ?? prometheus/              #    ? Configura��o do Prometheus
?   ?       ??? prometheus.yml          #    ? Configura��o de scraping
?   ??? .dockerignore                   #    ? Arquivos a ignorar no build
??? ?? scripts/                         # ?? Scripts de automa��o
?   ??? docker-manage.sh                #    ? Gerenciamento principal
?   ??? Makefile                        #    ? Comandos Make
?   ??? setup.sh                        #    ? Setup inicial do ambiente
?   ??? cleanup-root.sh                 #    ? Limpeza da raiz (migration)
?   ??? aliases.sh                      #    ? Aliases opcionais para terminal
??? ?? src/                             # ?? C�digo fonte
??? ?? tests/                           # ?? Projetos de teste
??? ?? docs/                            # ?? Documenta��o
??? README.md                           # ?? Documenta��o principal
??? (pasta raiz limpa!)                 # ? Sem arquivos Docker/scripts soltos
```

## ?? Migra��o da Estrutura Antiga

### Antes (Pasta Raiz Polu�da)
```
SalesAPI/
??? docker-compose.yml                  # ? Na raiz
??? docker-compose-observability-simple.yml # ? Na raiz  
??? docker-manage.sh                    # ? Na raiz
??? Makefile                            # ? Na raiz
??? observability/                      # ? Na raiz
??? src/
??? tests/
??? docs/
??? README.md
```

### Depois (Organizada)
```
SalesAPI/
??? ?? docker/compose/                  # ? Organizado
??? ?? docker/observability/            # ? Organizado  
??? ?? scripts/                         # ? Organizado
??? ?? src/
??? ?? tests/
??? ?? docs/
??? README.md                           # ? Raiz limpa!
```

## ?? Como Usar a Nova Estrutura

### 1. Setup Inicial
```bash
# Primeira vez no projeto
./scripts/setup.sh

# Verificar que tudo funciona
./scripts/docker-manage.sh status
```

### 2. Comandos Principais

#### Script de Gerenciamento Docker
```bash
# Comandos essenciais
./scripts/docker-manage.sh start     # Iniciar todos os servi�os
./scripts/docker-manage.sh stop      # Parar todos os servi�os
./scripts/docker-manage.sh status    # Ver status dos servi�os
./scripts/docker-manage.sh health    # Verificar sa�de dos servi�os
./scripts/docker-manage.sh urls      # Mostrar URLs dos servi�os

# Comandos de desenvolvimento
./scripts/docker-manage.sh logs      # Ver logs
./scripts/docker-manage.sh test      # Rodar testes de integra��o
./scripts/docker-manage.sh clean     # Limpeza de recursos

# Ajuda
./scripts/docker-manage.sh help      # Ver todos os comandos
```

#### Comandos Make (Alternativa)
```bash
# Comandos essenciais
make -C scripts up                   # Iniciar servi�os
make -C scripts down                 # Parar servi�os
make -C scripts status               # Ver status
make -C scripts test                 # Rodar testes

# Comandos especiais
make -C scripts observability        # Iniciar com monitoramento
make -C scripts quick-start          # Setup completo
make -C scripts clean                # Limpeza

# Ajuda
make -C scripts help                 # Ver todos os comandos
```

### 3. Aliases Opcionais

Para quem usa o terminal frequentemente:

```bash
# Carregar aliases
source scripts/aliases.sh

# Usar comandos curtos
dapi-start                           # = ./scripts/docker-manage.sh start
dapi-stop                            # = ./scripts/docker-manage.sh stop  
dapi-status                          # = ./scripts/docker-manage.sh status
dapi-logs                            # = ./scripts/docker-manage.sh logs-follow
dapi-test                            # = ./scripts/docker-manage.sh test
```

## ?? Para Desenvolvedores

### Novos no Projeto
1. **Clone o reposit�rio**
2. **Execute `./scripts/setup.sh`** - Verifica depend�ncias e configura ambiente
3. **Execute `./scripts/docker-manage.sh start`** - Inicia todos os servi�os
4. **Acesse http://localhost:6000** - Gateway da API

### Desenvolvedores Existentes
1. **Execute `./scripts/cleanup-root.sh`** - Remove arquivos antigos da raiz
2. **Use os novos comandos** - Funcionam igual, s� mudaram de lugar
3. **Atualize scripts/automa��es** - Se tiverem caminhos hardcoded

## ?? Compara��o de Comandos

| A��o | Comando Antigo | Novo Comando |
|------|----------------|-------------|
| Iniciar servi�os | `./docker-manage.sh start` | `./scripts/docker-manage.sh start` |
| Parar servi�os | `./docker-manage.sh stop` | `./scripts/docker-manage.sh stop` |
| Ver status | `make status` | `make -C scripts status` |
| Compose b�sico | `docker compose up` | `docker compose -f docker/compose/docker-compose.yml up` |
| Observabilidade | `docker compose -f docker-compose-observability-simple.yml up` | `docker compose -f docker/compose/docker-compose-observability-simple.yml up` |

## ? Benef�cios Implementados

### ?? Organiza��o
- **Pasta raiz limpa** - Apenas arquivos essenciais
- **Agrupamento l�gico** - Docker com Docker, scripts com scripts
- **Estrutura escal�vel** - F�cil adicionar novos componentes

### ?? Usabilidade  
- **Scripts inteligentes** - Detectam automaticamente os compose files
- **Comandos centralizados** - Um script para todas as opera��es
- **Aliases opcionais** - Comandos mais curtos para usu�rios frequentes

### ?? Manutenibilidade
- **Caminhos relativos** - Scripts funcionam de qualquer lugar
- **Valida��o autom�tica** - Verificam configura��es antes de executar
- **Logs organizados** - Outputs coloridos e informativos

### ?? Colabora��o
- **Onboarding simplificado** - Setup script guia novos desenvolvedores
- **Documenta��o atualizada** - README reflete nova estrutura
- **Padr�es consistentes** - Todos usam mesmos scripts

## ?? Solu��o de Problemas

### Scripts n�o executam
```bash
# Tornar scripts execut�veis
chmod +x scripts/*.sh
```

### Compose files n�o encontrados
```bash
# Validar configura��o
./scripts/docker-manage.sh validate
```

### Migra��o de arquivos antigos
```bash
# Usar script de limpeza
./scripts/cleanup-root.sh
```

### Caminhos n�o funcionam
```bash
# Executar sempre da raiz do projeto
cd /caminho/para/SalesAPI
./scripts/docker-manage.sh start
```

## ?? Resultado Final

? **Pasta raiz limpa e organizada**  
?? **Docker files centralizados em `docker/`**  
?? **Scripts organizados em `scripts/`**  
?? **Observabilidade configurada em `docker/observability/`**  
?? **Comandos simplificados e intuitivos**  
?? **Documenta��o atualizada**  

---

*Estrutura implementada para melhor organiza��o e manutenibilidade do projeto SalesAPI*