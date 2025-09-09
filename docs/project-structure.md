# ?? Estrutura Organizacional do SalesAPI

## ?? Objetivos da Reorganização

Esta reorganização foi implementada para:

? **Limpar a pasta raiz** - Mover arquivos Docker e scripts para pastas dedicadas  
? **Melhorar a organização** - Agrupar arquivos relacionados logicamente  
? **Facilitar a manutenção** - Localização intuitiva de configurações  
? **Padronizar estrutura** - Seguir boas práticas de projetos Enterprise  
? **Simplificar navegação** - Estrutura mais clara para novos desenvolvedores  

## ?? Nova Estrutura de Pastas

```
SalesAPI/
??? ?? docker/                          # ?? Tudo relacionado ao Docker
?   ??? ?? compose/                     #    Arquivos Docker Compose
?   ?   ??? docker-compose.yml          #    ? Serviços principais
?   ?   ??? docker-compose-observability-simple.yml # ? Com monitoramento
?   ??? ?? observability/               #    Configurações de monitoramento
?   ?   ??? ?? prometheus/              #    ? Configuração do Prometheus
?   ?       ??? prometheus.yml          #    ? Configuração de scraping
?   ??? .dockerignore                   #    ? Arquivos a ignorar no build
??? ?? scripts/                         # ?? Scripts de automação
?   ??? docker-manage.sh                #    ? Gerenciamento principal
?   ??? Makefile                        #    ? Comandos Make
?   ??? setup.sh                        #    ? Setup inicial do ambiente
?   ??? cleanup-root.sh                 #    ? Limpeza da raiz (migration)
?   ??? aliases.sh                      #    ? Aliases opcionais para terminal
??? ?? src/                             # ?? Código fonte
??? ?? tests/                           # ?? Projetos de teste
??? ?? docs/                            # ?? Documentação
??? README.md                           # ?? Documentação principal
??? (pasta raiz limpa!)                 # ? Sem arquivos Docker/scripts soltos
```

## ?? Migração da Estrutura Antiga

### Antes (Pasta Raiz Poluída)
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
./scripts/docker-manage.sh start     # Iniciar todos os serviços
./scripts/docker-manage.sh stop      # Parar todos os serviços
./scripts/docker-manage.sh status    # Ver status dos serviços
./scripts/docker-manage.sh health    # Verificar saúde dos serviços
./scripts/docker-manage.sh urls      # Mostrar URLs dos serviços

# Comandos de desenvolvimento
./scripts/docker-manage.sh logs      # Ver logs
./scripts/docker-manage.sh test      # Rodar testes de integração
./scripts/docker-manage.sh clean     # Limpeza de recursos

# Ajuda
./scripts/docker-manage.sh help      # Ver todos os comandos
```

#### Comandos Make (Alternativa)
```bash
# Comandos essenciais
make -C scripts up                   # Iniciar serviços
make -C scripts down                 # Parar serviços
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
1. **Clone o repositório**
2. **Execute `./scripts/setup.sh`** - Verifica dependências e configura ambiente
3. **Execute `./scripts/docker-manage.sh start`** - Inicia todos os serviços
4. **Acesse http://localhost:6000** - Gateway da API

### Desenvolvedores Existentes
1. **Execute `./scripts/cleanup-root.sh`** - Remove arquivos antigos da raiz
2. **Use os novos comandos** - Funcionam igual, só mudaram de lugar
3. **Atualize scripts/automações** - Se tiverem caminhos hardcoded

## ?? Comparação de Comandos

| Ação | Comando Antigo | Novo Comando |
|------|----------------|-------------|
| Iniciar serviços | `./docker-manage.sh start` | `./scripts/docker-manage.sh start` |
| Parar serviços | `./docker-manage.sh stop` | `./scripts/docker-manage.sh stop` |
| Ver status | `make status` | `make -C scripts status` |
| Compose básico | `docker compose up` | `docker compose -f docker/compose/docker-compose.yml up` |
| Observabilidade | `docker compose -f docker-compose-observability-simple.yml up` | `docker compose -f docker/compose/docker-compose-observability-simple.yml up` |

## ? Benefícios Implementados

### ?? Organização
- **Pasta raiz limpa** - Apenas arquivos essenciais
- **Agrupamento lógico** - Docker com Docker, scripts com scripts
- **Estrutura escalável** - Fácil adicionar novos componentes

### ?? Usabilidade  
- **Scripts inteligentes** - Detectam automaticamente os compose files
- **Comandos centralizados** - Um script para todas as operações
- **Aliases opcionais** - Comandos mais curtos para usuários frequentes

### ?? Manutenibilidade
- **Caminhos relativos** - Scripts funcionam de qualquer lugar
- **Validação automática** - Verificam configurações antes de executar
- **Logs organizados** - Outputs coloridos e informativos

### ?? Colaboração
- **Onboarding simplificado** - Setup script guia novos desenvolvedores
- **Documentação atualizada** - README reflete nova estrutura
- **Padrões consistentes** - Todos usam mesmos scripts

## ?? Solução de Problemas

### Scripts não executam
```bash
# Tornar scripts executáveis
chmod +x scripts/*.sh
```

### Compose files não encontrados
```bash
# Validar configuração
./scripts/docker-manage.sh validate
```

### Migração de arquivos antigos
```bash
# Usar script de limpeza
./scripts/cleanup-root.sh
```

### Caminhos não funcionam
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
?? **Documentação atualizada**  

---

*Estrutura implementada para melhor organização e manutenibilidade do projeto SalesAPI*