namespace GM.Application.Console.Model
{
    public class Console_Entity
    {
        private string _enable = string.Empty;
        private int _page_number = 1;
        private int _record_per_page = 999999;
        private string _create_by = "Console";

        public string CreateBy
        {
            get { return _create_by; }
            set { _create_by = value; }
        }

        public string Enable
        {
            get { return _enable; }
            set { _enable = value; }
        }

        public int PageNumber
        {
            get { return _page_number; }
            set { _page_number = value; }
        }

        public int RecordPerPage
        {
            get { return _record_per_page; }
            set { _record_per_page = value; }
        }

    }
}