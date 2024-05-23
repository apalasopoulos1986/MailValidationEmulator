using ARSoft.Tools.Net.Dns;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MailValidationEmulator
{
    public class MailValidator
    {
        public int ValidateAddress(string emailAddress)
        {
            ValidateEmailFormat(emailAddress);
            ValidateEmailObject(emailAddress);
            var mxRecord = ValidateMxRecord(emailAddress);

            int result = 200;
            result = RunVRFY(emailAddress, mxRecord);

            return result;
        }

        private void ValidateEmailFormat(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress) || !emailAddress.Contains("@") || !emailAddress.Contains("."))
                throw new InvalidOperationException("Invalid email address format.");

            try
            {
                emailAddress = Regex.Replace(emailAddress, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                string DomainMapper(Match match)
                {
                    var idn = new IdnMapping();

                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }

            try
            {
                if (!Regex.IsMatch(emailAddress,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
                {
                    throw new InvalidOperationException("Invalid email address format.");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                throw;
            }

            try
            {
                string[] host = emailAddress.Split('@');
                string address = host[0];

               // var invalidCharacterPattern = @"[\-/\\ &_'+=,<>]";

                var invalidCharacterPattern = @"[\-/\\ &'+=,<>]";
                if (!Regex.IsMatch(address, invalidCharacterPattern))
                {
                    if (address.Contains("..") || address.StartsWith(".") || address.EndsWith("."))
                        throw new InvalidOperationException("Invalid email address format.");

                    return;
                }
                else
                    throw new InvalidOperationException("Invalid email address format.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ValidateEmailObject(string emailAddress)
        {
            try
            {
                new MailAddress(emailAddress);

                var isValid = new EmailAddressAttribute().IsValid(emailAddress);

                if (!isValid)
                {
                    throw new InvalidOperationException("Invalid email.");
                }

                return;
            }
            catch (ArgumentNullException)
            {
                throw new InvalidOperationException("Invalid email address.");
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Invalid email address.");
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Invalid email address format.");
            }
            catch
            {
                throw;
            }
        }

        private MxRecord? ValidateMxRecord(string emailAddress)
        {
            try
            {
                string[] host = emailAddress.Split('@');
                string hostname = host[1];

                var resolver = new DnsStubResolver();
                var records = resolver.Resolve<MxRecord>(hostname, RecordType.Mx);

                if (records == null || !records.Any())
                    throw new InvalidOperationException("No mail exchange records found.");

                return records.First();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private int RunVRFY(string emailAddress, MxRecord mxRecord)
        {
            try
            {
                if (mxRecord == null)
                    return 400;

                using (TcpClient client = new TcpClient(mxRecord.ExchangeDomainName.ToString(), 25))
                using (StreamReader reader = new StreamReader(client.GetStream()))
                using (StreamWriter writer = new StreamWriter(client.GetStream()))
                {
                    //Initial response
                    string response = reader.ReadLine();

                    //Server is ready
                    if (!response.StartsWith("220"))
                        return 400;

                    //VRFY command
                    writer.WriteLine($"VRFY {emailAddress}");
                    writer.Flush();

                    response = reader.ReadLine();

                    if (response.StartsWith("250"))
                        return 250; // Email address is valid
                    else if (response.StartsWith("252"))
                        return 252; // Cannot verify the address, but it's not necessarily invalid
                    else
                        return 400; // Email address is invalid
                }
            }
            catch
            {
                return 400;
            }
        }
    }
}

