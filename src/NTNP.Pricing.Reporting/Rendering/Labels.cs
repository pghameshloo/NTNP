namespace NTNP.Pricing.Reporting.Rendering;

/// <summary>Persian/English label pairs for the customer quotation template (Section 26: Persian, English, or bilingual).</summary>
internal sealed record Labels(
    string QuotationTitle,
    string QuotationNumber, string Revision, string IssueDate, string ValidUntil, string CustomerInformation,
    string CustomerCompany, string ProjectName, string RfqNumber, string ContactPerson, string Attention, string Subject,
    string CommercialSummary, string TotalRialPayable, string TotalForeignPayable, string QuotationCurrency, string Validity,
    string SellingRateBasis, string LineItems, string Row, string CellCode, string PanelDescription, string ProductFamily,
    string VoltageLevel, string Quantity, string Unit, string UnitPrice, string TotalPrice, string Currency,
    string GrandTotalRial, string GrandTotalForeign, string Rial, string CommercialTerms, string DeliveryTerms,
    string DeliveryPeriod, string DeliveryLocation, string PaymentTerms, string WarrantyTerms, string InspectionTerms,
    string PackingTerms, string TransportationTerms, string TaxesAndDuties, string CurrencyBasis,
    string ExchangeRateConditions, string ScopeExclusions, string TechnicalNotes, string CommercialNotes,
    string Signatures, string PreparedBy, string CommercialManager, string ManagingDirector, string CustomerAcceptance, string Of)
{
    public static Labels For(bool fa) => fa ? Persian : English;

    private static readonly Labels Persian = new(
        QuotationTitle: "پیشنهاد فنی و مالی",
        QuotationNumber: "شماره پیشنهاد", Revision: "بازنگری", IssueDate: "تاریخ صدور", ValidUntil: "اعتبار تا",
        CustomerInformation: "اطلاعات مشتری", CustomerCompany: "شرکت مشتری", ProjectName: "نام پروژه", RfqNumber: "شماره استعلام",
        ContactPerson: "شخص رابط", Attention: "توجه", Subject: "موضوع", CommercialSummary: "خلاصه مالی",
        TotalRialPayable: "مبلغ قابل پرداخت ریالی", TotalForeignPayable: "مبلغ قابل پرداخت ارزی", QuotationCurrency: "ارز پیشنهاد",
        Validity: "مدت اعتبار", SellingRateBasis: "مبنای نرخ ارز فروش", LineItems: "اقلام پیشنهاد", Row: "ردیف",
        CellCode: "کد سلول", PanelDescription: "شرح تابلو", ProductFamily: "خانواده محصول", VoltageLevel: "سطح ولتاژ",
        Quantity: "تعداد", Unit: "واحد", UnitPrice: "قیمت واحد", TotalPrice: "قیمت کل", Currency: "ارز",
        GrandTotalRial: "جمع کل ریالی", GrandTotalForeign: "جمع کل ارزی", Rial: "ریال", CommercialTerms: "شرایط بازرگانی",
        DeliveryTerms: "شرایط تحویل", DeliveryPeriod: "مدت تحویل", DeliveryLocation: "محل تحویل", PaymentTerms: "شرایط پرداخت",
        WarrantyTerms: "شرایط گارانتی", InspectionTerms: "شرایط بازرسی", PackingTerms: "شرایط بسته‌بندی",
        TransportationTerms: "شرایط حمل", TaxesAndDuties: "مالیات و عوارض", CurrencyBasis: "مبنای ارزی",
        ExchangeRateConditions: "شرایط نرخ ارز", ScopeExclusions: "موارد خارج از تعهدات", TechnicalNotes: "ملاحظات فنی",
        CommercialNotes: "ملاحظات بازرگانی", Signatures: "امضاها", PreparedBy: "تهیه‌کننده", CommercialManager: "مدیر بازرگانی",
        ManagingDirector: "مدیرعامل", CustomerAcceptance: "تأیید مشتری", Of: "از");

    private static readonly Labels English = new(
        QuotationTitle: "Technical & Commercial Proposal",
        QuotationNumber: "Quotation No.", Revision: "Revision", IssueDate: "Issue Date", ValidUntil: "Valid Until",
        CustomerInformation: "Customer Information", CustomerCompany: "Customer Company", ProjectName: "Project Name",
        RfqNumber: "RFQ No.", ContactPerson: "Contact Person", Attention: "Attention", Subject: "Subject",
        CommercialSummary: "Commercial Summary", TotalRialPayable: "Total Rial Payable", TotalForeignPayable: "Total Foreign Payable",
        QuotationCurrency: "Quotation Currency", Validity: "Validity", SellingRateBasis: "Selling Rate Basis",
        LineItems: "Line Items", Row: "Row", CellCode: "Cell Code", PanelDescription: "Panel Description",
        ProductFamily: "Product Family", VoltageLevel: "Voltage Level", Quantity: "Qty", Unit: "Unit",
        UnitPrice: "Unit Price", TotalPrice: "Total Price", Currency: "Currency", GrandTotalRial: "Grand Total (Rial)",
        GrandTotalForeign: "Grand Total (Foreign)", Rial: "IRR", CommercialTerms: "Commercial Terms",
        DeliveryTerms: "Delivery Terms", DeliveryPeriod: "Delivery Period", DeliveryLocation: "Delivery Location",
        PaymentTerms: "Payment Terms", WarrantyTerms: "Warranty", InspectionTerms: "Inspection", PackingTerms: "Packing",
        TransportationTerms: "Transportation", TaxesAndDuties: "Taxes & Duties", CurrencyBasis: "Currency Basis",
        ExchangeRateConditions: "Exchange Rate Conditions", ScopeExclusions: "Scope Exclusions", TechnicalNotes: "Technical Notes",
        CommercialNotes: "Commercial Notes", Signatures: "Signatures", PreparedBy: "Prepared By",
        CommercialManager: "Commercial Manager", ManagingDirector: "Managing Director", CustomerAcceptance: "Customer Acceptance",
        Of: "of");
}
