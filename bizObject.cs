using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CPUFramework
{
    public class bizObject <T> : INotifyPropertyChanged where T:bizObject<T>, new()
    {
        string _typename = "";string _tablename = ""; string _getspoc = ""; string _updatespoc = ""; string _deletespoc = "";
        string _primarykeyname = ""; string _primarykeyparamname = "";
        DataTable _datatable = new();
        List<PropertyInfo> _properties = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bizObject()
        {
            Type t = this.GetType();
            _typename = t.Name;
            _tablename = _typename;
            if (_tablename.ToLower().StartsWith("biz")) { _tablename = _tablename.Substring(3); }
            _getspoc = _tablename + "Get";
            _updatespoc = _tablename + "Update";
            _deletespoc = _tablename + "Delete";
            _primarykeyname = _tablename + "Id";
            _primarykeyparamname = "@" + _primarykeyname;
            _properties = t.GetProperties().ToList<PropertyInfo>();
        }
        public DataTable Load(int primarykeyvalue)
        {
            DataTable dt = new();
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getspoc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, primarykeyvalue);
            dt = SQLUtility.GetDataTable(cmd);
            if(dt.Rows.Count > 0)
            {
                LoadProps(dt.Rows[0]);
            }
            _datatable = dt;
            return dt;
        }

        public List<T> GetList(bool includeblank = false)
        {
            List<T> lst = new();
            
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getspoc);
            SQLUtility.SetParamValue(cmd, "@All", 1);
            SQLUtility.SetParamValue(cmd, "@IncludeBlank", includeblank);
            var dt = SQLUtility.GetDataTable(cmd);
            return GetListDataTable(dt);
        }

        protected List<T> GetListDataTable(DataTable dt)
        {
            List<T> lst = new();
            foreach (DataRow dr in dt.Rows)
            {
                T obj = new T();
                obj.LoadProps(dr);
                lst.Add(obj);
            }
            return lst;

        }
        public void Delete(int id)
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(_deletespoc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, id);
            SQLUtility.ExecuteSQL(cmd);
        }

        private void LoadProps(DataRow dr)
        {
            foreach(DataColumn col in dr.Table.Columns)
            {
                SetProp(col.ColumnName, dr[col.ColumnName]);
              
            }
        }

        public void Delete()
        {
            PropertyInfo? prop = GetProp(_primarykeyname, true, false);
            if (prop != null)
            {
                object? id = prop.GetValue(this);             
                if (id != null)
                {
                    this.Delete((int)id);
                }
            }
        }
        public void Delete(DataTable datatable)
        {
            int id = (int)datatable.Rows[0][_primarykeyname];
            this.Delete(id);

        }

        public void Save()
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(_updatespoc);
            foreach(SqlParameter param in cmd.Parameters)
            {
                var prop = GetProp(param.ParameterName, true, false);
                if(prop != null)
                {
                    object? val = prop.GetValue(this);
                    if (val == null) { val = DBNull.Value; }
                    param.Value = val;
                }

            }
            SQLUtility.ExecuteSQL(cmd);
            foreach (SqlParameter param in cmd.Parameters)
            {
                if (param.Direction == ParameterDirection.InputOutput)
                {
                    SetProp(param.ParameterName, param.Value);
                }
            }
        }

        public void Save(DataTable datatable)
        {
            if (datatable.Rows.Count == 0)
            {
                throw new Exception($"Cannot call {_tablename} Save method becuase there are no rows in the table");
            }
            DataRow r = datatable.Rows[0];
            SQLUtility.SaveDataRow(r, _updatespoc);
        }

        private PropertyInfo? GetProp(string propname, bool forread, bool forwrite)
        {
            propname = propname.ToLower();
            if (propname.StartsWith("@")) { propname = propname.Substring(1); }
            PropertyInfo? prop = _properties.FirstOrDefault(p => 
                p.Name.ToLower() == propname 
                && (forread == false || p.CanRead == true)
                && (forwrite == false || p.CanWrite == true)
            );
            return prop;
        }
        private void SetProp(string propname, object? value)
        {
            var prop = GetProp(propname, false, true);
            if(prop != null)
            {
                if(value == DBNull.Value) { value = null; }
                try
                {
                    prop.SetValue(this, value);
                }
                catch(Exception ex)
                {
                    string msg = $"{_typename}.{prop.Name} is being set to {value?.ToString()} and that is the wrong data type. {ex.Message}";
                    throw new CPUDevExeption(msg, ex);
                }
               
            }
        }

        protected string GetSprocName { get => _getspoc; }
        protected void InvokePropertyChanged([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
