namespace Form1204
{
    public class PersonInfo
    {
        public string Fio { get; set; } = "";
        public string Aud { get; set; } = "";
    }

    public enum RowTag { Ok, NotFound, WrongAud, Long15, Long30 }
}
