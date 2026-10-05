# Home Library — «Домашняя библиотека»

Тестовое задание 5.2: веб-приложение для учёта книг домашней библиотеки.

- **Платформа:** ASP.NET Core Razor Pages, .NET 10.
- **СУБД:** PostgreSQL. Доступ к данным только через хранимые процедуры и функции, вызов через Dapper.
- **Архитектура:** Clean Architecture с соблюдением принципов SOLID.

## Возможности

- **Список книг** с постраничным выводом и поиском. Поиск ищет подстроку без учёта регистра в названии, авторе и оглавлении; области поиска выбираются флажками. Фраза в оглавлении находится, даже если её части по-разному отформатированы: например, «Глава 3» найдётся в `<strong>Глава</strong> 3`.
- **Карточка книги.** Поля:
  - название, автор, год издания;
  - ISBN, издательство, количество страниц, жанр, заметки;
  - даты создания и изменения;
  - оглавление.
- **Создание, редактирование и удаление** книг. Проверки выполняются и на клиенте, и на сервере.
- **Оглавление:**
  - редактируется в HTML-редакторе TinyMCE;
  - хранится в поле типа `xml`;
  - выгружается в XML-файл кнопкой «Download XML» в карточке. Файл повторяет хранимый XML; к корневому элементу добавлены атрибуты `bookId`, `title` и `author`.
- **Защита от одновременного редактирования.** Если книгу изменил кто-то другой, сохранение не затрёт его правки: пользователь увидит предупреждение, а введённые им данные останутся в форме.
- **Защита от XSS.** HTML оглавления очищается при сохранении и при выводе: остаются только структурные теги без атрибутов. У неподдерживаемых элементов (ссылок, таблиц) остаётся только текст, как в редакторе; исполняемое содержимое (`script`, `style`, `iframe`...) удаляется целиком.

## Архитектура

```
src/
  HomeLibrary.Domain          сущности, правила проверки, объект-значение TableOfContents (без внешних зависимостей)
  HomeLibrary.Application     сценарии (команды и запросы с обработчиками), интерфейсы репозиториев и конвертера
  HomeLibrary.Infrastructure  PostgreSQL: репозитории на Dapper, миграции FluentMigrator;
                              конвертер HTML ↔ XML; построение XML-файла выгрузки
  HomeLibrary.Web             Razor Pages, TinyMCE, фильтр 404, сборка зависимостей
tests/
  HomeLibrary.Domain.Tests          модульные тесты правил
  HomeLibrary.Application.Tests     модульные тесты обработчиков (NSubstitute)
  HomeLibrary.Infrastructure.Tests  интеграционные тесты на PostgreSQL, тесты конвертера и выгрузки
  HomeLibrary.Web.Tests             тесты страниц через WebApplicationFactory (постраничный вывод, 404, выгрузка)
```

Зависимости направлены только внутрь: Web → Application → Domain, Infrastructure → Application. Web ссылается на Infrastructure только как точка сборки зависимостей: в `Program.cs` регистрирует сервисы и запускает миграции.

Application разложен по функциям: всё, что относится к книгам, лежит в `Books/`, а внутри — по роли:

```
HomeLibrary.Application/
  Abstractions/              ICommandHandler, IQueryHandler
  Common/                    PagedResult
  Books/
    Models/                  BookInput, BookView, BookListItem, BookSearchCriteria, BookSearchScope, TableOfContentsFile
    Ports/                   интерфейсы, которые реализует Infrastructure
    Services/                IBookDetailsFactory, BookDetailsFactory
    Mappers/                 BookViewMapper (Book → BookView)
    Exceptions/              BookNotFoundException, BookConcurrencyException, TableOfContentsNotFoundException
    UseCases/                по папке на сценарий: команда или запрос и его обработчик
      CreateBook/  UpdateBook/  DeleteBook/  GetBook/  SearchBooks/  ExportTableOfContents/
```

- Интерфейсы для внешнего мира (`Books/Ports`):
  - `IBookReadRepository` — чтение книг;
  - `IBookWriteRepository` — изменение книг;
  - `ITableOfContentsConverter` — преобразование оглавления HTML ↔ XML;
  - `ITableOfContentsFileBuilder` — построение файла выгрузки.
- Реализации этих интерфейсов находятся в Infrastructure.
- Каждый сценарий — отдельный обработчик: `CreateBookHandler`, `UpdateBookHandler`, `DeleteBookHandler`, `GetBookHandler`, `SearchBooksHandler`, `ExportTableOfContentsHandler`.
- Тесты Application повторяют эту структуру (`tests/HomeLibrary.Application.Tests/Books/...`).

## База данных

Схема `library` создаётся и обновляется автоматически при запуске приложения миграциями FluentMigrator. Миграции — классы с методами `Up` и `Down` в `src/HomeLibrary.Infrastructure/Persistence/Migrations/Versions`, SQL в них записан прямо в коде. Применённые миграции отмечаются в таблице `library.version_info`. Имя схемы берётся из настройки `Database:Schema` (по умолчанию `library`), поэтому тесты разворачивают те же миграции во временных схемах.

| Объект | Тип | Назначение |
|---|---|---|
| `book` | таблица | Книги; оглавление в поле `toc xml`, его текст для поиска — в `toc_text` |
| `book_insert` | процедура | Добавление книги, возвращает `id` через `INOUT`-параметр |
| `book_update` | процедура | Изменение книги с проверкой версии строки |
| `book_delete` | процедура | Удаление книги |
| `book_get` | функция `RETURNS TABLE` | Получение карточки книги |
| `book_search` | функция `RETURNS TABLE` | Поиск с постраничным выводом и общим числом найденных книг |
| `book_toc_text`, `normalize_search_text` | функции | Текст оглавления для поиска: текст без тегов, неразрывные пробелы и повторяющиеся пробелы сведены к одному обычному |

Миграции (номер в формате `yyyyMMddHHmmss`; у первых трёх это метки, сохраняющие порядок скриптов DbUp, новые миграции получают время создания; номер применённой миграции не меняется):
- `20260925232000` `CreateBookTable` — таблица `book` и индексы; при первом запуске создаёт расширение `pg_trgm`, если его ещё нет. `Down` расширение не удаляет: оно общее для всех схем базы;
- `20260925232100` `CreateBookRoutines` — процедуры и функции;
- `20260926113000` `NormalizeTocSearchText` — нормализация текста для поиска фраз в оглавлении.

**Переход с DbUp.** Раньше миграции выполнял DbUp, и его журнал хранился в таблице `library.schema_versions`. Если при запуске эта таблица найдена, её записи переносятся в `version_info` (уже применённые скрипты не выполняются повторно, время применения переводится из местного в UTC, как пишет FluentMigrator), а сама таблица удаляется — всё в одной транзакции. Незнакомое имя скрипта в старом журнале останавливает запуск, журнал при этом не трогается.

**Почему чтение сделано функциями:** процедура PostgreSQL не может вернуть набор строк, поэтому для выборок используются функции.

**Ошибки процедур:** при отсутствии книги процедура выдаёт код `HL404`, при конфликте версий — `HL409`. Репозиторий превращает их в исключения `BookNotFoundException` и `BookConcurrencyException`.

**Индексы:** для быстрого поиска подстроки по `title`, `author` и `toc_text` построены GIN-индексы на расширении `pg_trgm`.

## Запуск

Требования:
- .NET SDK 10;
- PostgreSQL 13 или новее;
- база данных, в которой пользователь приложения может создавать схемы.

1. Задайте строку подключения. Пароль в репозиторий не попадает:

   ```bash
   dotnet user-secrets set "Database:ConnectionString" \
     "Host=localhost;Port=5432;Database=home_library;Username=<user>;Password=<password>" \
     --project src/HomeLibrary.Web
   ```

   Вместо этого можно задать переменную окружения `Database__ConnectionString`.

   Необязательные параметры пула соединений Npgsql дописываются в ту же строку, например `...;Maximum Pool Size=50;Minimum Pool Size=5`:

   | Параметр | По умолчанию | Смысл |
   |---|---|---|
   | `Pooling` | `true` | Включить пул соединений |
   | `Minimum Pool Size` | `0` | Сколько соединений держать открытыми постоянно |
   | `Maximum Pool Size` | `100` | Предел пула; при исчерпании запрос ждёт свободное соединение до `Timeout` |
   | `Connection Idle Lifetime` | `300` сек | Через сколько закрывать простаивающие соединения сверх минимума |
   | `Connection Pruning Interval` | `10` сек | Как часто проверять простаивающие соединения |
   | `Timeout` | `15` сек | Сколько ждать соединения, в том числе свободного соединения из пула |

   Сумма `Maximum Pool Size` всех экземпляров приложения не должна превышать `max_connections` сервера PostgreSQL (по умолчанию 100).

2. Запустите приложение:

   ```bash
   dotnet run --project src/HomeLibrary.Web --launch-profile http
   ```

   Приложение откроется по адресу http://localhost:5282. Схема БД будет создана при первом запуске.

## Тесты

```bash
dotnet test
```

- Интеграционные и веб-тесты берут строку подключения из тех же user-secrets (или из `Database__ConnectionString`).
- Для каждого прогона тесты создают временные схемы `library_test_<guid>`, `library_webtest_<guid>` и `library_migration_test_<guid>` (по одной на каждый тест миграций) и удаляют их после завершения.

## Сторонние компоненты

| Компонент | Лицензия | Назначение |
|---|---|---|
| Npgsql | PostgreSQL License | Драйвер PostgreSQL |
| Dapper | Apache 2.0 | Преобразование результатов запросов в объекты |
| FluentMigrator | Apache 2.0 | Миграции БД |
| HtmlSanitizer, AngleSharp | MIT | Очистка HTML и преобразование его в XHTML |
| TinyMCE 7.9.3 | GPL v2+ | HTML-редактор оглавления; локальная копия в `wwwroot/lib/tinymce` |
| Bootstrap 5.3.3 (включает Popper 2) | MIT | Оформление интерфейса; локальная копия в `wwwroot/lib/bootstrap` — только `bootstrap.min.css`, `bootstrap.bundle.min.js` и их карты исходников |
| jQuery 3.7.1, jQuery Validation 1.21.0, jQuery Validation Unobtrusive 4.0.0 | MIT | Проверки на клиенте, подключаются только на страницах добавления и редактирования книги; локальные копии в `wwwroot/lib` — только минифицированные файлы |

Файлы в `wwwroot/lib` побайтово совпадают с опубликованными npm-пакетами указанных версий: `.gitattributes` отключает для этой папки преобразование переводов строк.
