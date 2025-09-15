# Messaging Architecture

## ?? **Vis�o Geral**

Esta arquitetura de messaging foi projetada para ser profissional, escal�vel e seguir as melhores pr�ticas da ind�stria, **sem implementa��es fake no c�digo de produ��o**.

## ??? **Arquitetura por Ambiente**

### **Production Environment**
- ? **RealEventPublisher** com Rebus/RabbitMQ
- ? Configurado via `appsettings.Production.json`
- ? Suporte a ambiente-espec�fico
- ? **Messaging.Enabled = true**

### **Development Environment**  
- ? **NullEventPublisher** quando messaging desabilitado
- ? **RealEventPublisher** quando messaging habilitado
- ? Configura��o flex�vel via `appsettings.Development.json`
- ? **Messaging.Enabled = false** (padr�o para desenvolvimento local)

### **Testing Environment**
- ? **MockEventPublisher** isolado na infraestrutura de testes
- ? Localizado em `tests/*/TestInfrastructure/Mocks/`
- ? **MessagingTestFixture** para configura��o de testes
- ? **Completamente separado** do c�digo de produ��o

## ?? **Configura��o**

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

## ?? **Implementa��es**

### **Production Implementations**
| Servi�o | Implementa��o | Localiza��o | Uso |
|---------|---------------|-------------|-----|
| **Sales API** | `RealEventPublisher` | `src/sales.api/Services/EventPublisher/RealEventPublisher.cs` | Produ��o com Rebus |
| **Inventory API** | `InventoryEventPublisher` | `src/inventory.api/Configuration/MessagingConfiguration.cs` | Produ��o com Rebus |

### **Null Object Pattern** (Messaging Desabilitado)
| Servi�o | Implementa��o | Localiza��o | Uso |
|---------|---------------|-------------|-----|
| **Sales API** | `NullEventPublisher` | `src/sales.api/Configuration/MessagingConfiguration.cs` | Messaging desabilitado |
| **Inventory API** | `NullEventPublisher` | `src/inventory.api/Configuration/MessagingConfiguration.cs` | Messaging desabilitado |

### **Test Implementations**
| Servi�o | Implementa��o | Localiza��o | Uso |
|---------|---------------|-------------|-----|
| **Testes** | `MockEventPublisher` | `tests/SalesAPI.Tests.Professional/TestInfrastructure/Mocks/` | Somente testes |
| **Test Fixture** | `MessagingTestFixture` | `tests/SalesAPI.Tests.Professional/TestInfrastructure/Fixtures/` | Configura��o de testes |

## ?? **Princ�pios da Arquitetura**

### **? C�digo Limpo**
- **Sem implementa��es fake em produ��o**
- **Separa��o clara** entre c�digo de produ��o e teste
- **Null Object Pattern** para cen�rios de messaging desabilitado
- **Factory Pattern** para cria��o de implementa��es

### **? Configura��o Flex�vel**
- **Configura��o por ambiente** via appsettings
- **Habilita��o/desabilita��o** via configura��o
- **Par�metros espec�ficos** por ambiente (workers, connection strings)
- **Suporte a m�ltiplos ambientes**

### **? Testabilidade**
- **MockEventPublisher** isolado nos testes
- **MessagingTestFixture** para configura��o de testes
- **Event capture e verification** nos testes
- **Limpeza autom�tica** entre testes

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

## ??? **Valida��es de Arquitetura**

### **? Implementa��es Removidas**
- ~~`DummyEventPublisher.cs`~~ (Sales API)
- ~~`DummyEventPublisher.cs`~~ (Inventory API)
- ~~`MockEventPublisher.cs`~~ (Sales API - movido para testes)
- ~~`DevMockEventPublisher`~~ (EventPublisherFactory)

### **? Implementa��es Atuais**
- `RealEventPublisher` (Sales API)
- `InventoryEventPublisher` (Inventory API)
- `NullEventPublisher` (ambos, quando messaging desabilitado)
- `MockEventPublisher` (apenas em testes)

## ?? **Como Usar**

### **Para Produ��o**
1. **Configurar** `Messaging.Enabled = true`
2. **Definir** connection string do RabbitMQ
3. **Configurar** workers conforme necess�rio
4. **Deploy** com configura��es de produ��o

### **Para Desenvolvimento**
1. **Manter** `Messaging.Enabled = false` para desenvolvimento local
2. **Habilitar** `Messaging.Enabled = true` para testar com RabbitMQ
3. **Configurar** RabbitMQ local se necess�rio

### **Para Testes**
1. **Usar** `MessagingTestFixture` nos testes
2. **Verificar** eventos capturados via `MockEventPublisher`
3. **Limpar** eventos entre testes com `ClearPublishedEvents()`

## ? **Benef�cios Alcan�ados**

| Aspecto | Benef�cio |
|---------|-----------|
| **Qualidade** | Zero implementa��es fake em produ��o |
| **Manuten��o** | Configura��o centralizada e flex�vel |
| **Testabilidade** | Infraestrutura de testes isolada e robusta |
| **Escalabilidade** | Suporte a m�ltiplos ambientes e configura��es |
| **Profissionalismo** | Arquitetura production-ready |

---
*Documenta��o atualizada: Dezembro 2024*  
*Arquitetura implementada seguindo best practices da ind�stria*