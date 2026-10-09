namespace ADAICA_AUTOTEST.Pages;

/// <summary>Чат двох користувачів: назва, перше повідомлення автора чату й відповідь учасника.</summary>
public sealed record ChatData(string Name, string Message, string Reply);
