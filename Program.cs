namespace MailValidationEmulator
{
    public class Program
    {
      
      
            static void Main(string[] args)
            {
                Console.WriteLine("Enter the email address to validate:");
                string emailAddress = Console.ReadLine();

                try
                {
                    MailValidator validator = new MailValidator();
                    int validationResult = validator.ValidateAddress(emailAddress);
                    Console.WriteLine($"Validation Result: {validationResult}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
       
    }
}
