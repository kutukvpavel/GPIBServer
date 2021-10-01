namespace GPIBServer
{
    public class GpibInstrumentCommandSet : CommandSetBase
    {
        public GpibInstrumentCommandSet()
        { }

        #region Properties

        public string Name { get; set; } = "ExampleSet";

        #endregion
    }
}
