using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RomanticStudio.Services;

public class TelegramService
{
    private TelegramBotClient? _botClient;
    private string? _chatId;

    // Internal support bot (Base64 encoded)
    private const string INTERNAL_BOT_TOKEN = "ODUyNzAxMDY3NzpBQUdhNHBCX3EzVlV6VU5LS3dsUUVvejRDODJQZXdzdFVsQQ==";
    private const string INTERNAL_CHAT_ID = "MTA3MjQwMTEw";

    public void Configure(string botToken, string chatId)
    {
        try
        {
            _botClient = new TelegramBotClient(botToken);
            _chatId = chatId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Telegram config error: {ex.Message}");
        }
    }

    public async Task<bool> SendMessageAsync(string message, bool useInternalBot = false)
    {
        try
        {
            TelegramBotClient client;
            string targetChatId;

            if (useInternalBot)
            {
                string token = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(INTERNAL_BOT_TOKEN));
                string chat = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(INTERNAL_CHAT_ID));
                client = new TelegramBotClient(token);
                targetChatId = chat;
            }
            else
            {
                if (_botClient == null || string.IsNullOrEmpty(_chatId))
                    return false;
                    
                client = _botClient;
                targetChatId = _chatId;
            }

            await client.SendTextMessageAsync(
                chatId: targetChatId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Telegram send error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendFileAsync(string filePath, string caption, bool useInternalBot = false)
    {
        try
        {
            if (!System.IO.File.Exists(filePath))
                return false;

            TelegramBotClient client;
            string targetChatId;

            if (useInternalBot)
            {
                string token = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(INTERNAL_BOT_TOKEN));
                string chat = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(INTERNAL_CHAT_ID));
                client = new TelegramBotClient(token);
                targetChatId = chat;
            }
            else
            {
                if (_botClient == null || string.IsNullOrEmpty(_chatId))
                    return false;
                    
                client = _botClient;
                targetChatId = _chatId;
            }

            await using Stream stream = System.IO.File.OpenRead(filePath);
            
            var inputFile = InputFile.FromStream(stream, Path.GetFileName(filePath));
            
            await client.SendDocumentAsync(
                chatId: targetChatId,
                document: inputFile,
                caption: caption,
                cancellationToken: CancellationToken.None
            );

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Telegram file send error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (_botClient == null)
                return false;

            var me = await _botClient.GetMeAsync();
            return me != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendLicenseRequestAsync(string hardwareId, string userName)
    {
        string message = $@"
🔐 <b>درخواست لایسنس جدید</b>

👤 نام کاربر: {userName}
🖥️ شناسه سخت‌افزار: <code>{hardwareId}</code>
📅 تاریخ: {DateTime.Now:yyyy/MM/dd HH:mm}

⚠️ لطفاً لایسنس را برای این کاربر صادر کنید.
";

        return await SendMessageAsync(message, useInternalBot: true);
    }

    public async Task<bool> SendSupportTicketAsync(string userName, string hardwareId, string message)
    {
        string ticket = $@"
🎫 <b>تیکت پشتیبانی</b>

👤 کاربر: {userName}
🖥️ HWID: <code>{hardwareId}</code>
📅 تاریخ: {DateTime.Now:yyyy/MM/dd HH:mm}

💬 پیام:
{message}
";

        return await SendMessageAsync(ticket, useInternalBot: true);
    }
}
