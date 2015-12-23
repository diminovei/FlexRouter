namespace FlexRouter.Hardware.Arcc
{
    internal enum ArccAxisCommand
    {
        EnableDisableAxis = 1
        //  Byte - mean (5 bytes length)
        //  01 - group
        //  XX - Id
        //  XX - Command
        //  XX - Axis number
        //  XX - 0 - Disable, 1 - enable
        // ---------------------------------
        //  Byte - mean (18 bytes length)
        //  01 - group
        //  XX - Id
        //  XX XX - Axis 8
        //  XX XX - Axis 7
        //  XX XX - Axis 6
        //  XX XX - Axis 5
        //  XX XX - Axis 4
        //  XX XX - Axis 3
        //  XX XX - Axis 2
        //  XX XX - Axis 1
    }
}