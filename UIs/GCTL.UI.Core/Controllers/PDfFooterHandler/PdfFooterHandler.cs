using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;


namespace GCTL.Service.PDfFooterHandler
{
    public class PdfFooterHandler : AbstractPdfDocumentEventHandler
    {
        #region Service

        private readonly PdfFont _font;
        private readonly PdfFormXObject _totalPagesPlaceholder;

        public PdfFooterHandler(
            PdfFont font,
            PdfFormXObject totalPagesPlaceholder)
        {
            _font = font;
            _totalPagesPlaceholder = totalPagesPlaceholder;
        }

        #endregion

        #region Print Date & Time And Page Number

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
            float rightX = pageSize.GetWidth() - 30;

            int pageNumber = pdf.GetPageNumber(page);

            string dateTime =
                DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");

            string leftText =
                $"Print Date & Time: {dateTime}";

            string rightText =
                $"Page {pageNumber} of ";

            canvas.ShowTextAligned(
                new Paragraph(leftText)
                    .SetFont(_font)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY),
                leftX,
                bottomY,
                TextAlignment.LEFT);

            canvas.ShowTextAligned(
                new Paragraph(rightText)
                    .SetFont(_font)
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY),
                rightX - 4,
                bottomY,
                TextAlignment.RIGHT);

            // Total page placeholder
            canvas.Add(
                new Image(_totalPagesPlaceholder)
                    .SetFixedPosition(
                        rightX,
                        bottomY + 2,
                        50));

            canvas.Close();
        }

        #endregion

        #region Counting total Page

        public void WriteTotalPages(PdfDocument pdfDoc)
        {
            int totalPages = pdfDoc.GetNumberOfPages();

            PdfCanvas canvas =
                new PdfCanvas(
                    _totalPagesPlaceholder,
                    pdfDoc);

            canvas.BeginText();
            canvas.SetFontAndSize(_font, 8);
            canvas.SetFillColor(ColorConstants.GRAY);
            canvas.MoveText(0, 0);
            canvas.ShowText(totalPages.ToString());
            canvas.EndText();
        }

        #endregion
    }
}
