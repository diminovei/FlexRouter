namespace FlexRouter.Hardware.Arcc
{
    public enum ArccBinaryInputCommand
    {
        SetUpFilter = 1,
        DumpAllLines = 2
        //  Byte - mean (5 bytes length)
        //  08 - group
        //  XX - Id
        //  XX - Command
        //  XX - Any data
        //  XX - Any data
    }
}