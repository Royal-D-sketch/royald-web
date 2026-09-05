import re
with open('RoyalD.Web/Views/Shared/_Layout.cshtml', 'r', encoding='utf-8') as f:
    content = f.read()

# I want to add condition blocks
# <a ... href=""/Debtor/Index""> -> wrap with if (await PermissionService.CanAccessAsync(""DebtorList"")) { ... }
# <a ... href=""/SalesBill/Index""> -> wrap with if (await PermissionService.CanAccessAsync(""SalesBillList"")) { ... }
# <a ... href=""/SalesReport/Summary""> -> wrap with if (await PermissionService.CanAccessAsync(""SalesRepReport"")) { ... }
# <a ... href=""/SalesReport/AnnualPerformance""> -> wrap with if (await PermissionService.CanAccessAsync(""AnnualPerformance"")) { ... }
# <a ... href=""/SalesReport/CustomerProduct""> -> wrap with if (await PermissionService.CanAccessAsync(""CustomerSalesReport"")) { ... }
# <a ... href=""/Report/WaitingGoods""> -> wrap with if (await PermissionService.CanAccessAsync(""WaitingGoodsReport"")) { ... }
# <a ... href=""/Upload/Index""> -> wrap with if (await PermissionService.CanAccessAsync(""Upload"")) { ... }
# <a ... href=""/Audit/Index""> -> wrap with if (await PermissionService.CanAccessAsync(""AuditLogs"")) { ... }
# I will just write a regex or replace explicitly.

replacements = {
    '/Debtor/Index': 'DebtorList',
    '/SalesBill/Index': 'SalesBillList',
    '/SalesReport/Summary': 'SalesRepReport',
    '/SalesReport/AnnualPerformance': 'AnnualPerformance',
    '/SalesReport/CustomerProduct': 'CustomerSalesReport',
    '/Report/WaitingGoods': 'WaitingGoodsReport',
    '/Upload/Index': 'Upload',
    '/Audit/Index': 'AuditLogs'
}

for href, key in replacements.items():
    # Find <a class=""nav-link ..."" href=""href"">...</a>
    # Use regex
    pattern = r'(<a[^>]*href=""' + href + r'""[^>]*>.*?</a>)'
    replacement = r'@if (await PermissionService.CanAccessAsync(""' + key + r'"")) { \1 }'
    content = re.sub(pattern, replacement, content, flags=re.DOTALL)

# Let's also add Admin Settings Link inside the menu
admin_menu = r'''
                    @if (await PermissionService.CanAccessAsync("AdminPermissions"))
                    {
                        <hr class="dropdown-divider my-2 border-secondary" style="opacity:0.2;">
                        <a class="nav-link" href="/Admin/Permissions">
                            <i class="bi bi-shield-lock"></i> ตั้งค่าสิทธิ์เมนู
                        </a>
                    }
'''
content = content.replace('</nav>', admin_menu + '</nav>')

with open('RoyalD.Web/Views/Shared/_Layout.cshtml', 'w', encoding='utf-8') as f:
    f.write(content)

