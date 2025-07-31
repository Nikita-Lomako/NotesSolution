# Руководство по использованию Cancellation Token в NotesSolution

## Обзор

Этот документ описывает реализацию и использование Cancellation Token в проекте NotesSolution, следуя лучшим практикам из статьи [Глубокое погружение в CancellationToken](https://habr.com/ru/companies/simbirsoft/articles/825386/).

## Архитектура

### 1. ICancellationTokenProvider

Централизованный провайдер для управления токенами отмены:

```csharp
public interface ICancellationTokenProvider
{
    CancellationToken GetDefaultToken();
    CancellationTokenSource CreateTimeoutTokenSource(int timeoutMs);
    CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens);
}
```

### 2. CancellationTokenProvider

Реализация провайдера с безопасным управлением ресурсами:

- Получает токен из HttpContext.RequestAborted
- Создает токены с таймаутом
- Создает связанные токены для комбинирования нескольких источников отмены

## Использование в сервисах

### Пример использования в NoteService

```csharp
public async Task<List<NoteDto>> GetAllNotes(string userId, string? search, string? tag, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken = default)
{
    try
    {
        // Создаем связанный токен с таймаутом
        using var timeoutTokenSource = _cancellationTokenProvider.CreateTimeoutTokenSource(30000); // 30 секунд
        using var linkedTokenSource = _cancellationTokenProvider.CreateLinkedTokenSource(
            cancellationToken, 
            timeoutTokenSource.Token, 
            _cancellationTokenProvider.GetDefaultToken());

        var combinedToken = linkedTokenSource.Token;

        // Проверяем отмену перед операцией
        combinedToken.ThrowIfCancellationRequested();

        var notes = await _noteRepository.GetAllAsync(userId, search, tag, sort, order, page, pageSize, combinedToken);
        return _mapper.Map<List<NoteDto>>(notes);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Operation was cancelled for user {UserId}", userId);
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting notes for user {UserId}", userId);
        throw;
    }
}
```

## Безопасность и предотвращение проблем

### 1. Утечки памяти

**Проблема:** Неправильная обработка токенов отмены может привести к "зомби-процессам".

**Решение:**
- Используем `using` для автоматического освобождения ресурсов
- Регулярно проверяем `cancellationToken.ThrowIfCancellationRequested()`
- Правильно обрабатываем `OperationCanceledException`

### 2. Блокировки и взаимоблокировки

**Проблема:** Неправильная синхронизация может привести к блокировкам.

**Решение:**
- Используем связанные токены для координации отмены
- Избегаем блокирующих операций в критических секциях
- Правильно обрабатываем исключения отмены

### 3. Некорректное управление ресурсами

**Проблема:** Ресурсы могут не освобождаться при отмене операции.

**Решение:**
- Используем `using` для всех ресурсов
- Обрабатываем исключения в `try-catch-finally`
- Освобождаем ресурсы даже при отмене

### 4. Обработка исключений

**Проблема:** Некорректная обработка `TaskCanceledException`.

**Решение:**
- Явно обрабатываем `OperationCanceledException`
- Логируем отмены операций
- Возвращаем понятные ошибки клиенту

### 5. Общее использование CancellationTokenSource

**Проблема:** Использование одного токена для множества задач.

**Решение:**
- Создаем отдельные токены для каждой операции
- Используем связанные токены для координации
- Отслеживаем жизненный цикл токенов

### 6. Производительность

**Проблема:** Частые проверки токена могут снизить производительность.

**Решение:**
- Проверяем токен перед длительными операциями
- Используем разумные таймауты
- Балансируем между отзывчивостью и производительностью

## Таймауты по операциям

| Операция | Таймаут | Примечание |
|----------|---------|------------|
| Получение заметок | 30 сек | Стандартная операция |
| Получение одной заметки | 15 сек | Быстрая операция |
| Создание заметки | 60 сек | Включает обработку изображений |
| Обновление заметки | 60 сек | Включает обработку изображений |
| Удаление заметки | 30 сек | Включает удаление изображений |
| Аутентификация | 15 сек | Быстрая операция |
| Работа с тегами | 10-30 сек | В зависимости от сложности |

## Логирование

Все операции с Cancellation Token логируются:

```csharp
_logger.LogWarning("Operation was cancelled for user {UserId}", userId);
_logger.LogDebug("Creating timeout token source with {TimeoutMs}ms timeout", timeoutMs);
_logger.LogInformation("Successfully completed operation for user {UserId}", userId);
```

## Обработка в контроллерах

В эндпоинтах правильно обрабатываем отмены:

```csharp
try
{
    var result = await service.OperationAsync(cancellationToken);
    return Results.Ok(result);
}
catch (OperationCanceledException)
{
    return Results.StatusCode(499); // Client Closed Request
}
catch (Exception ex)
{
    return Results.Problem("An error occurred", statusCode: 500);
}
```

## Регистрация сервисов

В `Program.cs` зарегистрированы все необходимые сервисы:

```csharp
// Регистрация провайдера токенов отмены
builder.Services.AddScoped<ICancellationTokenProvider, CancellationTokenProvider>();

// Обновленные сервисы с поддержкой Cancellation Token
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IAuthService, AuthService>();
```

## Тестирование

При тестировании учитывайте:

1. **Мок CancellationTokenProvider** для изоляции тестов
2. **Проверка отмены операций** с помощью `CancellationTokenSource`
3. **Валидация освобождения ресурсов** при отмене
4. **Тестирование таймаутов** для длительных операций

## Заключение

Реализация Cancellation Token в NotesSolution следует лучшим практикам:

- ✅ Безопасное управление ресурсами
- ✅ Правильная обработка исключений
- ✅ Централизованное управление токенами
- ✅ Логирование всех операций
- ✅ Настраиваемые таймауты
- ✅ Предотвращение утечек памяти

Эта реализация обеспечивает надежную и отзывчивую работу приложения, позволяя пользователям отменять длительные операции и предотвращая блокировку ресурсов. 