namespace Form1204;

/// <summary>Данные о человеке из БД по штрихкоду.</summary>
public class PersonInfo
{
    public string Fio { get; init; } = "";
    public string Aud { get; init; } = "";
}

/// <summary>Одна строка таблицы (позиция бланка на странице).</summary>
public class SheetRow
{
    public string ImageNo  { get; init; } = "";
    public string Page     { get; init; } = "";
    public string Pos      { get; init; } = "";
    public string Barcode  { get; init; } = "";
    public string Fio      { get; init; } = "";
    public string OutTime  { get; init; } = "";
    public string InTime   { get; init; } = "";
    public int    Minutes  { get; init; }
    public string DbAud    { get; init; } = "";
    public string Status   { get; init; } = "";
    public RowTag Tag      { get; init; }
}

public enum RowTag { Ok, NotFound, WrongAud, Long15, Long30 }
