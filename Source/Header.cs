namespace HttpKit
{
    /// <summary>
    /// 
    /// </summary>
    public class Header
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public Header(string value)
        {
            Name = string.Empty;
            Value = string.Empty;

            // TODO: Use index of ? 
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] == ':')
                {
                    Name = value.Substring(0, index);
                    Value = value.Substring(index + 1).Trim();
                    break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Value);
        }
    }
}
