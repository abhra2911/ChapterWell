namespace Lib_Mgmt.Data.Entities
{
    /// <summary>Maps to LibMgmt_Librarians. PK is application-assigned (no identity/sequence).</summary>
    public class Librarian
    {
        public int LibrarianId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string LibrarianCode { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int IsActive { get; set; }   // NUMBER(1): 1 = active, 0 = inactive
    }
}
