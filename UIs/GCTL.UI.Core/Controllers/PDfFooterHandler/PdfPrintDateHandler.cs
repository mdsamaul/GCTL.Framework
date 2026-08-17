using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace GCTL_App.Controllers.PDfFooterHandler
{
    public class PdfPrintDateHandler : AbstractPdfDocumentEventHandler
    {
        #region Service

        private readonly PdfFont _font;

        public PdfPrintDateHandler(PdfFont font)
        {
            _font = font;
        }

        #endregion

        #region Print Date & Time

        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
        {
            var pdfEvent = (PdfDocumentEvent)@event;

            var pdf = pdfEvent.GetDocument();
            var page = pdfEvent.GetPage();
            var pageSize = page.GetPageSize();

            var canvas = new Canvas(
                new PdfCanvas(
                    page.NewContentStreamBefore(),
                    page.GetResources(),
                    pdf),
                pageSize);

            float bottomY = 12;
            float leftX = 30;

            string dateTime =
                DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");

            string text =
                $"Print Date & Time: {dateTime}";

            canvas.ShowTextAligned(
                new Paragraph(text)
                    .SetFont(_font)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY),
                leftX,
                bottomY,
                TextAlignment.LEFT
            );

            canvas.Close();
        }

        #endregion
    }
}
