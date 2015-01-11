namespace FlexRouter.MessagesToMainForm
{
    /// <summary>
    /// Список сообщений главной форме
    /// </summary>
    public enum MessageToMainForm
    {
        RouterStarted,              //  Роутер запущен
        RouterStopped,              //  Роутер остановлен
        ClearConnectedDevicesList,  //  Очистить список подключенных устройств
        ChangeConnectedDevice,         //  Добавить подключенное устройство
        ShowEvent,                  //  Показать сработавший контрол
        NewHardwareEvent,
        RouterPaused
    }
}