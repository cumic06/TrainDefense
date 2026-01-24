#if UNITY_EDITOR
using NPOI.SS.UserModel;

namespace TrainDefense.Editor.DataImport.Importers.Rows
{
    public class TrainUpgradeRow : IExcelRow
    {
        public string id;
        public string name;
        public string description;
        public int maxHp;
        public string iconId;

        public virtual void FromExcelRow(IRow row)
        {
            id = row.GetCell(0)?.ToString();
            name = row.GetCell(1)?.ToString();
            description = row.GetCell(2)?.ToString();
            int.TryParse(row.GetCell(3)?.ToString(), out maxHp);
            iconId = row.GetCell(4)?.ToString();
        }

        public virtual void ToExcelRow(IRow row)
        {
            Set(row, 0, id);
            Set(row, 1, name);
            Set(row, 2, description);
            Set(row, 3, maxHp);
            Set(row, 4, iconId);
        }

        protected static void Set(IRow row, int idx, object value)
        {
            var cell = row.GetCell(idx) ?? row.CreateCell(idx);
            if (value is null) cell.SetCellValue(string.Empty);
            else if (value is int i) cell.SetCellValue(i);
            else if (value is bool b) cell.SetCellValue(b);
            else if (value is float f) cell.SetCellValue(f);
            else cell.SetCellValue(value.ToString());
        }
    }
}
#endif


