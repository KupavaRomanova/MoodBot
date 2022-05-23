using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

string[] moodEmojies = 
{
    "😃", "🥱", "😐", "😌", "😬", "😔", "😠", "😉", "🙄", "☹", "😤", "🤒", "🤨", "😜", "🥰",
};

string[] numbersEmojies =
{
    "0⃣", "1⃣", "2⃣", "3⃣", "4⃣", "5⃣", "6⃣", "7⃣", "8⃣", "9⃣"
};


// Создаем и запускаем бота
var botClient = new TelegramBotClient("5267781353:AAGJOjkQQ0dVQ8gMvRdr2gqnoCDXXo79Y9E");
var users = new Dictionary<long, UserData>();


// Обработчик сообщений
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandlePollingErrorAsync
);

// Получаем его имя и выводим в консоль
var me = await botClient.GetMeAsync();
Console.WriteLine($"I'm work.\nID: {me.Id} Name: {me.FirstName}.");

// Запрещаем боту выключаться, пока не нажмем Enter
Console.ReadLine();

ReplyKeyboardMarkup GetKeyboard(UserData userData)
{
    var date = DateTime.Now;

    return new(new[]
    {
 
        new KeyboardButton("Отметить настроение") ,
        new KeyboardButton("Посмотреть календарь"),
    })
    {
        ResizeKeyboard = true
    };
}

ReplyKeyboardMarkup GetMoodsKeyboard()
{

    var moods = Enum.GetNames(typeof(Mood));
    var keyboard = new KeyboardButton[moods.Length][];
    

    for (int i = 0; i < moods.Length; i++)
    {
        keyboard[i] = new[] { new KeyboardButton(
            $"{moodEmojies[i]} {moods[i]} {moodEmojies[i]}") };
    }


    return new ReplyKeyboardMarkup(keyboard)
    {
        ResizeKeyboard = true
    };
}

string MakeCalendar(UserData userData)
{
    string str = "#⃣#⃣❄❄🌸🌸🌸☀☀☀🍂🍂🍂❄\n";
    int year = DateTime.Now.Year;

    for (int i = 0; i < 31; i++)
    {

        str += numbersEmojies[(i+1) / 10] + numbersEmojies[(i+1) % 10];
        for (int j = 0; j < 12; j++)
        {
            try
            {

                var dateOnly = new DateTime(year, j + 1, i + 1);


                if (userData.Moods.ContainsKey(dateOnly))
                    str += moodEmojies[(int)userData.Moods[dateOnly]];
                else
                    str += "⬜";
            }
            catch
            {
                str += "⬜";
            }
        }
        str += "\n";
    }

    return str;
}

// Обработчик сообщений и событий
async Task HandleUpdateAsync(

    ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{

    // Обрабатываем только сообщения
    if (update.Type != UpdateType.Message)
        return;

    // Обрабатываем только Текстовые сообщения
    if (update.Message!.Type != MessageType.Text)
        return;

    if (update.Message.From == null)
        return;

    var chatId = update.Message.Chat.Id;
    var fromId = update.Message.From!.Id;
    var messageText = update.Message.Text;
 
    Console.WriteLine(fromId);
    Console.WriteLine(messageText);
    Message sentMessage;

    // Если пользователя нет в списке, то добавляем его туда
    if (!users.ContainsKey(fromId))
    {
        users[fromId] = new UserData() { 
            FirstName = update.Message.From.FirstName,
            UserName = update.Message.From.Username
        };

        sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"Добро пожаловать в календарь настроения, {update.Message.From.FirstName}!\n",
            replyMarkup: GetKeyboard(users[fromId])
        );
        return;
    }

    // Выбираем пользователя по Id
    UserData userData = users[fromId];

    if (!userData.IsChoosingMood)
    {
        switch (messageText)
        {
            case "Отметить настроение":
            case "Изменить настроение":
                sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Отлично. Как вы себя чувствуете сегодня?",
                    replyMarkup: GetMoodsKeyboard()
                );
               
                userData.IsChoosingMood = true;
                break;
            case "Посмотреть календарь":
                sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: MakeCalendar(userData),
                    replyMarkup: GetKeyboard(users[fromId])
                );
                break;

        }
    } 
    else
    {
        try
        {

            var mood = (Mood)Enum.Parse(typeof(Mood), messageText.Substring(2, messageText.Length - 4));

            var now = DateTime.Now;

            var date = new DateTime(now.Year, now.Month, now.Day);

            userData.Moods[date] = mood;


            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Настроение {mood} на {date} успешно зафиксировано.",
                replyMarkup: GetKeyboard(userData)
            );

            

        }

        catch
        {
            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Произошла ошибка.",
                replyMarkup: GetKeyboard(userData)
            );
        }
        // Пользователь переходит в состояние главного меню
        userData.IsChoosingMood = false;

    }
    
}



Task HandlePollingErrorAsync(
    ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n" +
            $"{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}


public enum Mood
{
    Happy,
    Lazy,
    Neutral,
    Relaxed,
    Anxious,
    Depressed,
    Angry,
    Excited,
    Stressed,
    Sad,
    Nervous,
    Sick,
    Irritable,
    Silly,
    Loving
};

public class UserData
{
    private Dictionary<DateTime, Mood> moods = new Dictionary<DateTime, Mood>();

    public Dictionary<DateTime, Mood> Moods { get => moods; set => moods = value; }
    public bool IsChoosingMood { get; set; } = false;
    public string FirstName { get; set; } = "Unknown";
    public string UserName { get; set; } = "Unknown";
}






