using System;
using System.IO;
using Common;
using System.Data.SqlClient;

namespace ExigoService
{
    public static partial class Exigo
    {
        public static ExigoImageApi Images()
        {
            return new ExigoImageApi();
        }
    }

    public sealed class ExigoImageApi
    {
        private string LoginName = GlobalSettings.Exigo.Api.LoginName;
        private string Password = GlobalSettings.Exigo.Api.Password;

        public AvatarResponse GetCustomerAvatarResponse(int customerID, AvatarType type, bool cache = true, byte[] bytes = null)
        {
            var response = new AvatarResponse();

            var path = "/customers/" + customerID.ToString();
            var filename = "avatar";
            switch (type)
            {
                //case AvatarType.Tiny: filename += "-xs"; break;
                case AvatarType.Small: filename += "-sm"; break;
                case AvatarType.Large: filename += "-lg"; break;
            }

            // All images set to png, so we have to have this work around for now
            filename = filename + ".png";
            if (bytes == null)
            {
                using (var conn = new SqlConnection(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting))
                {
                    conn.Open();

                    var cmd = new SqlCommand("Select top 1 ImageData From ImageFiles Where Path=@FilePath AND Name=@FileName", conn);
                    cmd.Parameters.Add("@FilePath", System.Data.SqlDbType.NVarChar, 500).Value = path;
                    cmd.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 500).Value = filename;
                    bytes = (byte[])cmd.ExecuteScalar();
                }
            }

            response.Bytes = bytes;

            // If we didn't find anything there, convert the default image (which is Base64) to a byte array.
            // We'll use that instead
            if (bytes == null)
            {
                bytes = Convert.FromBase64String(GlobalSettings.Avatars.DefaultAvatarAsBase64);
            

                return GetCustomerAvatarResponse(customerID, type, cache, GlobalUtilities.ResizeImage(bytes, type));
            }
            else
            {

                var extension = Path.GetExtension(filename).ToLower();
                string contentType = "image/jpeg";
                switch (extension)
                {
                    case ".gif":
                        contentType = "image/gif";
                        break;
                    case ".jpeg":
                        contentType = "image/png";
                        break;
                    case ".bmp":
                        contentType = "image/bmp";
                        break;
                    case ".png":
                        contentType = "image/png";
                        break;
                    case ".jpg":
                    default:
                        contentType = "image/jpeg";
                        break;
                }

                response.FileType = contentType;
            }


            return response;
        }
        
        public bool SetCustomerAvatar(int customerID, byte[] bytes, bool saveToHistory = false)
        {
            return ((GlobalUtilities.InsertOrUpdateAvatarToAPI(customerID, bytes) ? GlobalUtilities.InsertOrUpdateAvatarToReportingDatabase(customerID, bytes) : false));
        }

        public bool SaveImage(string path, string filename, byte[] bytes)
        {
            try
            {

                Exigo.WebService().SetImageFile(new Common.Api.ExigoWebService.SetImageFileRequest
                {
                    Name = filename,
                    Path = path,
                    ImageData = bytes
                });
            }
            catch (Exception ex)
            {
                return false;
            }

            // If we got here, everything should be good
            return true;
        }        
    }

    public class AvatarResponse
    {
        public byte[] Bytes { get; set; }
        public string FileType { get; set; }
    }
}