namespace Mesharp
{
	public class BadRequestReport
	{
		public BadRequestReport() {}

		public BadRequestReport (BadRequest badRequest)
		{
			BadRequest = badRequest;
		}

		public BadRequest BadRequest { get; set; }
	}
}