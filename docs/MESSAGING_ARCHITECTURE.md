# Messaging Architecture

## ?? **Visão Geral**

Esta arquitetura de messaging foi projetada para ser profissional, escalável e seguir as melhores práticas da indústria, **sem implementações fake no código de produção**.

## ??? **Arquitetura por Ambiente**

### **Production Environment**
- ? **RealEventPublisher** com Rebus/RabbitMQ
- ? Configurado via `appsettings.Production.json`
- ? Suporte a ambiente-específico
- ? **Messaging.Enabled = true**

### **Development Environment**  
- ? **NullEventPublisher** quando messaging desabilitado
- ? **RealEventPublisher** quando messaging habilitado
- ? Configuração flexível via `appsettings.Development.json`
- ? **Messaging.Enabled = false** (padrão para desenvolvimento local)

### **Testing Environment**
- ? **MockEventPublisher** isolado na infraestrutura de testes
- ? Localizado em `tests/*/TestInfrastructure/Mocks/`
- ? **MessagingTestFixture** para configuração de testes
- ? **Completamente separado** do código de produção

## ?? **Configuração**

### **Sales API**

#### Production (`appsettings.Production.json`)
```json
{
  "Messaging": {
    "Enabled": true,
    "ConnectionString": "amqp://user:password@localhost:5672",
    "Workers": 5
  }
}
```

#### Development (`appsettings.Development.json`)
```json
{
  "Messaging": {
    "Enabled": false,
    "ConnectionString": "amqp://admin:admin123@localhost:5672/",
    "Workers": 1
  }
}
```

### **Inventory API**

#### Production (`appsettings.Production.json`)
```json
{
  "Messaging": {
    "Enabled": true,
    "ConnectionString": "amqp://user:password@localhost:5672",
    "Workers": 5
  }
}
```

#### Development (`appsettings.Development.json`)
```json
{
  "Messaging": {
    "Enabled": false,
    "ConnectionString": "amqp://admin:admin123@localhost:5672/",
    "Workers": 1
  }
}
```

## ?? **Implementações**

### **Production Implementations**
| Serviço | Implementação | Localização | Uso |
|---------|---------------|-------------|-----|
| **Sales API** | `RealEventPublisher` | `src/sales.api/Services/EventPublisher/RealEventPublisher.cs` | Produção com Rebus |
| **Inventory API** | `InventoryEventPublisher` | `src/inventory.api/Configuration/MessagingConfiguration.cs` | Produção com Rebus |

### **Null Object Pattern** (Messaging Desabilitado)
| Serviço | Implementação | Localização | Uso |
|---------|---------------|-------------|-----|
| **Sales API** | `NullEventPublisher` | `src/sales.api/Configuration/MessagingConfiguration.cs` | Messaging desabilitado |
| **Inventory API** | `NullEventPublisher` | `src/inventory.api/Configuration/MessagingConfiguration.cs` | Messaging desabilitado |

### **Test Implementations**
| Serviço | Implementação | Localização | Uso |
|---------|---------------|-------------|-----|
| **Testes** | `MockEventPublisher` | `tests/SalesAPI.Tests.Professional/TestInfrastructure/Mocks/` | Somente testes |
| **Test Fixture** | `MessagingTestFixture` | `tests/SalesAPI.Tests.Professional/TestInfrastructure/Fixtures/` | Configuração de testes |

## ?? **Princípios da Arquitetura**

### **? Código Limpo**
- **Sem implementações fake em produção**
- **Separação clara** entre código de produção e teste
- **Null Object Pattern** para cenários de messaging desabilitado
- **Factory Pattern** para criação de implementações

### **? Configuração Flexível**
- **Configuração por ambiente** via appsettings
- **Habilitação/desabilitação** via configuração
- **Parâmetros específicos** por ambiente (workers, connection strings)
- **Suporte a múltiplos ambientes**

### **? Testabilidade**
- **MockEventPublisher** isolado nos testes
- **MessagingTestFixture** para configuração de testes
- **Event capture e verification** nos testes
- **Limpeza automática** entre testes

## ?? **Fluxo de Mensagens**

### **Sales API ? Inventory API**
```
Sales API (Order Confirmed)
    ? RealEventPublisher
    ? Rebus/RabbitMQ
    ? OrderConfirmedEvent
Inventory API (Order Processing)
```

### **Test Environment**
```
Test Method
    ? MockEventPublisher
    ? Event Capture (In-Memory)
    ? Test Assertions
Test Verification
```

## ??? **Validações de Arquitetura**

### **? Implementações Removidas**
- ~~`DummyEventPublisher.cs`~~ (Sales API)
- ~~`DummyEventPublisher.cs`~~ (Inventory API)
- ~~`MockEventPublisher.cs`~~ (Sales API - movido para testes)
- ~~`DevMockEventPublisher`~~ (EventPublisherFactory)

### **? Implementações Atuais**
- `RealEventPublisher` (Sales API)
- `InventoryEventPublisher` (Inventory API)
- `NullEventPublisher` (ambos, quando messaging desabilitado)
- `MockEventPublisher` (apenas em testes)

## ?? **Como Usar**

### **Para Produção**
1. **Configurar** `Messaging.Enabled = true`
2. **Definir** connection string do RabbitMQ
3. **Configurar** workers conforme necessário
4. **Deploy** com configurações de produção

### **Para Desenvolvimento**
1. **Manter** `Messaging.Enabled = false` para desenvolvimento local
2. **Habilitar** `Messaging.Enabled = true` para testar com RabbitMQ
3. **Configurar** RabbitMQ local se necessário

### **Para Testes**
1. **Usar** `MessagingTestFixture` nos testes
2. **Verificar** eventos capturados via `MockEventPublisher`
3. **Limpar** eventos entre testes com `ClearPublishedEvents()`

## ? **Benefícios Alcançados**

| Aspecto | Benefício |
|---------|-----------|
| **Qualidade** | Zero implementações fake em produção |
| **Manutenção** | Configuração centralizada e flexível |
| **Testabilidade** | Infraestrutura de testes isolada e robusta |
| **Escalabilidade** | Suporte a múltiplos ambientes e configurações |
| **Profissionalismo** | Arquitetura production-ready |

---
*Documentação atualizada: Dezembro 2024*  
*Arquitetura implementada seguindo best practices da indústria*