using System.Net;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Desktop.Services.Api;
using NTNP.Pricing.Desktop.Tests.TestSupport;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Tests;

/// <summary>Exercises the master/detail search-create-edit pattern shared by every master-data screen (Customers, Equipment, Currencies, etc.).</summary>
public class CustomersViewModelTests
{
    private static readonly CustomerDto SampleCustomer = new(
        Guid.NewGuid(), "CUST-001", "Acme Switchgear", "Industrial", null, null, "Jane Doe", "Buyer",
        "+98-21-000", "jane@example.com", "Tehran", null, true, "admin", DateTimeOffset.UtcNow, null, null, new byte[] { 1 });

    private static (CustomersViewModel ViewModel, FakeHttpMessageHandler Handler) Build()
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler);
        var serverSettings = new FakeServerConnectionSettingsService();
        var session = AuthenticatedSessionFactory.Create();
        var api = new CustomersApiClient(http, serverSettings, session);
        var dialogs = new FakeDialogService();
        return (new CustomersViewModel(api, dialogs), handler);
    }

    [Fact]
    public async Task OnNavigatedToAsync_LoadsCustomersIntoTheGrid()
    {
        var (vm, handler) = Build();
        handler.WhenJson(HttpMethod.Get, "api/customers", new PagedResult<CustomerDto>(new[] { SampleCustomer }, 1, 1, 200));

        await vm.OnNavigatedToAsync();

        var customer = Assert.Single(vm.Customers);
        Assert.Equal("Acme Switchgear", customer.CompanyName);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectingACustomer_PopulatesTheEditForm()
    {
        var (vm, handler) = Build();
        handler.WhenJson(HttpMethod.Get, "api/customers", new PagedResult<CustomerDto>(new[] { SampleCustomer }, 1, 1, 200));
        await vm.OnNavigatedToAsync();

        vm.SelectedCustomer = vm.Customers[0];

        Assert.True(vm.IsEditing);
        Assert.False(vm.IsNew);
        Assert.Equal("CUST-001", vm.FormCustomerCode);
        Assert.Equal("Jane Doe", vm.FormContactPerson);
    }

    [Fact]
    public void New_ClearsTheFormAndEntersCreateMode()
    {
        var (vm, _) = Build();

        vm.NewCommand.Execute(null);

        Assert.True(vm.IsNew);
        Assert.True(vm.IsEditing);
        Assert.Equal(string.Empty, vm.FormCompanyName);
    }

    [Fact]
    public async Task SaveAsync_WhenCreating_PostsTheRightPayload_AndSelectsTheCreatedRow()
    {
        var (vm, handler) = Build();
        var created = SampleCustomer with { Id = Guid.NewGuid(), CustomerCode = "CUST-999", CompanyName = "New Co." };

        handler.WhenJson(HttpMethod.Get, "api/customers", new PagedResult<CustomerDto>(new[] { created }, 1, 1, 200));
        handler.WhenJson(HttpMethod.Post, "api/customers", created, HttpStatusCode.Created);

        vm.NewCommand.Execute(null);
        vm.FormCustomerCode = "CUST-999";
        vm.FormCompanyName = "New Co.";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsEditing);
        Assert.NotNull(vm.SelectedCustomer);
        Assert.Equal("CUST-999", vm.SelectedCustomer!.CustomerCode);

        var postRequest = handler.Requests.Single(r => r.Method == HttpMethod.Post);
        var body = await postRequest.Content!.ReadAsStringAsync();
        Assert.Contains("CUST-999", body);
        Assert.Contains("New Co.", body);
    }

    [Fact]
    public async Task SaveAsync_WhenTheServerRejectsAConcurrencyConflict_SurfacesTheErrorMessage_AndStaysInEditMode()
    {
        var (vm, handler) = Build();
        handler.WhenJson(HttpMethod.Get, "api/customers", new PagedResult<CustomerDto>(new[] { SampleCustomer }, 1, 1, 200));
        await vm.OnNavigatedToAsync();
        vm.SelectedCustomer = vm.Customers[0];

        handler.When(HttpMethod.Put, "api/customers", _ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new ApiErrorResponse(
                "concurrency-conflict", "Concurrency conflict", 409, new[] { "This record was changed by another user. Reload it and re-apply your changes." })),
        });

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.IsEditing); // never silently drops the user's in-progress edit
        Assert.Contains("changed by another user", vm.ErrorMessage);
    }
}
