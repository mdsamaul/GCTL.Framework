namespace GCTL.Core.ViewModels.Email
{
    //TODO: Sf
    public class EmailSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string MailFrom { get; set; }
        public bool EnableSsl { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class EmailDataDto
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AttachmentPath { get; set; }
        public string AttachmentName { get; set; }
    }

    public class EmailResult
    {
        public string Email { get; set; }
        public bool isSuccess { get; set; }
        public string Error { get; set; }
    }

    public class EmailAttachmentDto
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
