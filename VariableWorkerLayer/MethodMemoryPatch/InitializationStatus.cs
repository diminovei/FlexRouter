namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    /// <summary>
    /// Статус инициализации модуля доступа к переменным
    /// </summary>
    public enum InitializationStatus
    {
        Ok,
        AttemptToInitializeTooOften,
        ModuleToPatchWasNotFound,
        Exception,
        MultipleModulesFound
    }
}