namespace GPIBServer
{
    public class GpibInstrument
    {
        #region Properties

        public string CommandSetName { get; set; } = "ExampleSet";
        public int Address { get; set; } = 16;
        public string Name { get; set; } = "Example 16";
        public string Type { get; set; } = "Type";

        #endregion
    }
}
