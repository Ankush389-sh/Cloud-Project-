using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ExpenseTrackerApp;

public partial class Form1 : Form
{
    // Color Palette
    private readonly Color BgDark = Color.FromArgb(18, 18, 18);
    private readonly Color CardDark = Color.FromArgb(30, 30, 30);
    private readonly Color PrimaryGreen = Color.FromArgb(40, 167, 69); // Money green
    private readonly Color PrimaryGreenHover = Color.FromArgb(33, 136, 56);
    private readonly Color SecondaryTeal = Color.FromArgb(32, 201, 151);
    private readonly Color DangerRed = Color.FromArgb(220, 53, 69);
    private readonly Color DangerRedHover = Color.FromArgb(200, 35, 51);
    private readonly Color TextWhite = Color.White;
    private readonly Color TextGrey = Color.LightGray;
    private readonly Color SidebarActive = Color.FromArgb(45, 45, 45);

    private DatabaseManager _dbManager;
    private int _selectedExpenseId = -1;

    // UI Panels
    private Panel _pnlSidebar;
    private Panel _pnlMain;
    private Panel _pnlDashboard;
    private Panel _pnlAddExpense;
    private Panel _pnlReports;
    private Panel _pnlCategories;

    // Sidebar Buttons
    private ModernButton _btnNavDashboard;
    private ModernButton _btnNavAdd;
    private ModernButton _btnNavReports;
    private ModernButton _btnNavCategories;

    // Dashboard Controls
    private Label _lblTotalAllTime;
    private Label _lblTotalMonth;
    private DataGridView _dgvRecent;
    private Chart _chartDashPie;
    private ComboBox _cbSearchCategory;
    private ComboBox _cbSearchTime;

    // Add Expense Controls
    private TextBox _txtAmount;
    private ComboBox _cbCategory;
    private DateTimePicker _dtpDate;
    private TextBox _txtDescription;
    private ModernButton _btnAddExpenseSave;
    
    // Reports Controls
    private Chart _chartReportsBar;
    private Chart _chartReportsPie;
    private Chart _chartReportsLine;
    private DateTimePicker _dtpReportFrom;
    private DateTimePicker _dtpReportTo;
    
    // Categories Controls
    private ListBox _lstCategories;
    private TextBox _txtNewCategory;

    // Manage/Edit
    private ModernButton _btnUpdate;
    private ModernButton _btnDelete;

    public Form1()
    {
        InitializeComponent();
        _dbManager = new DatabaseManager();
        SetupUI();
        SwitchPage(_pnlDashboard, _btnNavDashboard);
        LoadData();
    }

    private void SetupUI()
    {
        this.Text = "Expense Tracker";
        this.Size = new Size(1300, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BgDark;
        this.Font = new Font("Segoe UI", 10F);
        this.ForeColor = TextWhite;

        // --- SIDEBAR ---
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = CardDark
        };
        this.Controls.Add(_pnlSidebar);

        var lblLogo = new Label
        {
            Text = "💰 Expense\nTracker",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = PrimaryGreen,
            Dock = DockStyle.Top,
            Height = 100,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _pnlSidebar.Controls.Add(lblLogo);

        _btnNavDashboard = CreateNavButton("📊 Dashboard", 100);
        _btnNavAdd = CreateNavButton("➕ Add Expense", 150);
        _btnNavReports = CreateNavButton("📈 Reports", 200);
        _btnNavCategories = CreateNavButton("📂 Categories", 250);

        _btnNavDashboard.Click += (s, e) => SwitchPage(_pnlDashboard, _btnNavDashboard);
        _btnNavAdd.Click += (s, e) => { ClearAddForm(); SwitchPage(_pnlAddExpense, _btnNavAdd); };
        _btnNavReports.Click += (s, e) => SwitchPage(_pnlReports, _btnNavReports);
        _btnNavCategories.Click += (s, e) => SwitchPage(_pnlCategories, _btnNavCategories);

        _pnlSidebar.Controls.Add(_btnNavCategories);
        _pnlSidebar.Controls.Add(_btnNavReports);
        _pnlSidebar.Controls.Add(_btnNavAdd);
        _pnlSidebar.Controls.Add(_btnNavDashboard);

        // --- MAIN AREA ---
        _pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgDark,
            Padding = new Padding(20)
        };
        this.Controls.Add(_pnlMain);
        _pnlMain.BringToFront();

        BuildDashboardPage();
        BuildAddExpensePage();
        BuildReportsPage();
        BuildCategoriesPage();
    }

    private ModernButton CreateNavButton(string text, int y)
    {
        var btn = new ModernButton
        {
            Text = text,
            Location = new Point(0, y),
            Width = 220,
            Height = 50,
            FlatStyle = FlatStyle.Flat,
            BackColor = CardDark,
            ForeColor = TextGrey,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.HoverColor = SidebarActive;
        btn.NormalColor = CardDark;
        return btn;
    }

    private void SwitchPage(Panel targetPage, ModernButton activeButton)
    {
        _pnlDashboard.Visible = false;
        _pnlAddExpense.Visible = false;
        _pnlReports.Visible = false;
        _pnlCategories.Visible = false;

        _btnNavDashboard.BackColor = CardDark; _btnNavDashboard.ForeColor = TextGrey;
        _btnNavAdd.BackColor = CardDark; _btnNavAdd.ForeColor = TextGrey;
        _btnNavReports.BackColor = CardDark; _btnNavReports.ForeColor = TextGrey;
        _btnNavCategories.BackColor = CardDark; _btnNavCategories.ForeColor = TextGrey;

        targetPage.Visible = true;
        targetPage.Dock = DockStyle.Fill;
        
        activeButton.BackColor = SidebarActive;
        activeButton.ForeColor = TextWhite;
        
        LoadData(); // reload on switch
    }

    private void BuildDashboardPage()
    {
        _pnlDashboard = new Panel { Dock = DockStyle.Fill };
        _pnlMain.Controls.Add(_pnlDashboard);

        // Title Header Panel
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50 };
        _pnlDashboard.Controls.Add(pnlHeader);

        var lblDashTitle = new Label { Text = "Dashboard", Font = new Font("Segoe UI", 20F, FontStyle.Bold), Dock = DockStyle.Left, AutoSize = true };
        pnlHeader.Controls.Add(lblDashTitle);

        var btnQuickAdd = new ModernButton { 
            Text = "➕ Quick Add", 
            Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
            BackColor = PrimaryGreen, 
            NormalColor = PrimaryGreen, 
            HoverColor = PrimaryGreenHover, 
            ForeColor = TextWhite, 
            Width = 140, 
            Height = 40,
            FlatStyle = FlatStyle.Flat, 
            Dock = DockStyle.Right 
        };
        btnQuickAdd.FlatAppearance.BorderSize = 0;
        btnQuickAdd.Click += (s, e) => { ClearAddForm(); SwitchPage(_pnlAddExpense, _btnNavAdd); };
        pnlHeader.Controls.Add(btnQuickAdd);

        // Top Cards Panel
        var pnlCards = new Panel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(0, 10, 0, 10) };
        _pnlDashboard.Controls.Add(pnlCards);

        var cardTotal = new CardPanel { Width = 300, Height = 100, Location = new Point(0, 10), BackColor = CardDark };
        cardTotal.Controls.Add(new Label { Text = "Total Expenses (All Time)", ForeColor = TextGrey, Location = new Point(20, 20), AutoSize = true });
        _lblTotalAllTime = new Label { Text = "$0.00", Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = TextWhite, Location = new Point(20, 45), AutoSize = true };
        cardTotal.Controls.Add(_lblTotalAllTime);
        pnlCards.Controls.Add(cardTotal);

        var cardMonth = new CardPanel { Width = 300, Height = 100, Location = new Point(320, 10), BackColor = CardDark };
        cardMonth.Controls.Add(new Label { Text = "This Month's Expenses", ForeColor = TextGrey, Location = new Point(20, 20), AutoSize = true });
        _lblTotalMonth = new Label { Text = "$0.00", Font = new Font("Segoe UI", 22F, FontStyle.Bold), ForeColor = PrimaryGreen, Location = new Point(20, 45), AutoSize = true };
        cardMonth.Controls.Add(_lblTotalMonth);
        pnlCards.Controls.Add(cardMonth);

        // Middle Split (Chart + Table)
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 500 };
        split.Padding = new Padding(0, 15, 0, 0);
        _pnlDashboard.Controls.Add(split);
        split.BringToFront();

        // Pie Chart
        var cardChart = new CardPanel { Dock = DockStyle.Fill, BackColor = CardDark, Padding = new Padding(10) };
        cardChart.Controls.Add(new Label { Text = "Categories (This Month)", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
        
        _chartDashPie = new Chart { Dock = DockStyle.Fill, BackColor = CardDark };
        var chartArea = new ChartArea("DashArea") { BackColor = CardDark };
        _chartDashPie.ChartAreas.Add(chartArea);
        var series = new Series("Cats") { ChartType = SeriesChartType.Pie, IsValueShownAsLabel = true, LabelForeColor = TextWhite };
        _chartDashPie.Series.Add(series);
        
        var legend = new Legend("DashLegend") { BackColor = CardDark, ForeColor = TextWhite };
        _chartDashPie.Legends.Add(legend);
        
        cardChart.Controls.Add(_chartDashPie);
        _chartDashPie.BringToFront();
        split.Panel1.Controls.Add(cardChart);
        split.Panel1.Padding = new Padding(0, 0, 10, 0);

        // DataGridView
        var cardTable = new CardPanel { Dock = DockStyle.Fill, BackColor = CardDark, Padding = new Padding(10) };
        
        var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 35 };
        pnlFilter.Controls.Add(new Label { Text = "Recent Expenses", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 5) });
        
        _cbSearchCategory = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BgDark, ForeColor = TextWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F) };
        _cbSearchCategory.Items.Add("All Categories");
        _cbSearchCategory.SelectedIndex = 0;
        _cbSearchCategory.Location = new Point(200, 5);
        _cbSearchCategory.SelectedIndexChanged += (s, e) => ApplyDashboardFilter();
        pnlFilter.Controls.Add(_cbSearchCategory);

        _cbSearchTime = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BgDark, ForeColor = TextWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F) };
        _cbSearchTime.Items.AddRange(new string[] { "All Time", "Today", "This Week", "This Month" });
        _cbSearchTime.SelectedIndex = 0;
        _cbSearchTime.Location = new Point(370, 5);
        _cbSearchTime.SelectedIndexChanged += (s, e) => ApplyDashboardFilter();
        pnlFilter.Controls.Add(_cbSearchTime);
        cardTable.Controls.Add(pnlFilter);
        
        _dgvRecent = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = CardDark,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(50, 50, 50)
        };
        _dgvRecent.ColumnHeadersDefaultCellStyle.BackColor = BgDark;
        _dgvRecent.ColumnHeadersDefaultCellStyle.ForeColor = TextGrey;
        _dgvRecent.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _dgvRecent.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _dgvRecent.DefaultCellStyle.BackColor = CardDark;
        _dgvRecent.DefaultCellStyle.ForeColor = TextWhite;
        _dgvRecent.DefaultCellStyle.SelectionBackColor = SecondaryTeal;
        _dgvRecent.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
        _dgvRecent.RowTemplate.Height = 35;

        cardTable.Controls.Add(_dgvRecent);
        _dgvRecent.BringToFront();

        // Edit/Delete Action Panel in Dashboard
        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 10, 0, 0) };
        _btnUpdate = new ModernButton { Text = "Edit Selected", Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = SecondaryTeal, NormalColor = SecondaryTeal, HoverColor = Color.FromArgb(20, 180, 130), ForeColor = TextWhite, Width = 120, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Left };
        _btnDelete = new ModernButton { Text = "Delete", Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = DangerRed, NormalColor = DangerRed, HoverColor = DangerRedHover, ForeColor = TextWhite, Width = 100, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right };
        _btnUpdate.FlatAppearance.BorderSize = 0; _btnDelete.FlatAppearance.BorderSize = 0;
        
        _btnUpdate.Click += (s, e) => {
            if (_dgvRecent.SelectedRows.Count > 0) {
                var row = _dgvRecent.SelectedRows[0];
                _selectedExpenseId = Convert.ToInt32(row.Cells["Id"].Value);
                _dtpDate.Value = Convert.ToDateTime(row.Cells["Date"].Value);
                _cbCategory.Text = row.Cells["Category"].Value.ToString();
                _txtAmount.Text = row.Cells["Amount"].Value.ToString();
                _txtDescription.Text = row.Cells["Description"].Value?.ToString() ?? "";
                SwitchPage(_pnlAddExpense, _btnNavAdd);
            }
            else MessageBox.Show("Select an expense to edit.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        _btnDelete.Click += BtnDelete_Click;

        pnlActions.Controls.Add(_btnDelete);
        pnlActions.Controls.Add(_btnUpdate);
        cardTable.Controls.Add(pnlActions);

        split.Panel2.Controls.Add(cardTable);
        split.Panel2.Padding = new Padding(10, 0, 0, 0);
    }

    private void BuildAddExpensePage()
    {
        _pnlAddExpense = new Panel { Dock = DockStyle.Fill };
        _pnlMain.Controls.Add(_pnlAddExpense);

        var lblTitle = new Label { Text = "Add / Edit Expense", Font = new Font("Segoe UI", 20F, FontStyle.Bold), Dock = DockStyle.Top, Height = 60 };
        _pnlAddExpense.Controls.Add(lblTitle);

        var formCard = new CardPanel { Width = 800, Height = 480, BackColor = CardDark };
        // Center the card manually
        formCard.Location = new Point(20, 70);
        _pnlAddExpense.Controls.Add(formCard);

        int y = 30; int spacing = 75; int x = 40; int tbWidth = 720;

        // Amount
        formCard.Controls.Add(new Label { Text = "Amount ($)", ForeColor = TextGrey, Location = new Point(x, y), AutoSize = true });
        _txtAmount = new TextBox { Location = new Point(x, y + 25), Width = tbWidth, BackColor = BgDark, ForeColor = TextWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12F) };
        formCard.Controls.Add(_txtAmount);
        y += spacing;

        // Category
        formCard.Controls.Add(new Label { Text = "Category", ForeColor = TextGrey, Location = new Point(x, y), AutoSize = true });
        _cbCategory = new ComboBox { Location = new Point(x, y + 25), Width = tbWidth, BackColor = BgDark, ForeColor = TextWhite, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12F) };
        // Loaded dynamically
        formCard.Controls.Add(_cbCategory);
        y += spacing;

        // Date
        formCard.Controls.Add(new Label { Text = "Date", ForeColor = TextGrey, Location = new Point(x, y), AutoSize = true });
        _dtpDate = new DateTimePicker { Location = new Point(x, y + 25), Width = tbWidth, CalendarMonthBackground = BgDark, CalendarTitleBackColor = CardDark, Font = new Font("Segoe UI", 12F) };
        formCard.Controls.Add(_dtpDate);
        y += spacing;

        // Description
        formCard.Controls.Add(new Label { Text = "Description", ForeColor = TextGrey, Location = new Point(x, y), AutoSize = true });
        _txtDescription = new TextBox { Location = new Point(x, y + 25), Width = tbWidth, BackColor = BgDark, ForeColor = TextWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12F) };
        formCard.Controls.Add(_txtDescription);
        y += spacing + 20;

        // Save Button
        _btnAddExpenseSave = new ModernButton { Text = "Save Expense", Location = new Point(x, y), Width = tbWidth, Height = 45, BackColor = PrimaryGreen, NormalColor = PrimaryGreen, HoverColor = PrimaryGreenHover, Font = new Font("Segoe UI", 12F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        _btnAddExpenseSave.FlatAppearance.BorderSize = 0;
        _btnAddExpenseSave.Click += BtnAddExpenseSave_Click;
        formCard.Controls.Add(_btnAddExpenseSave);
        
        // Clear / Cancel Button
        var btnCancel = new ModernButton { Text = "Clear Form", Location = new Point(x, y + 60), Width = tbWidth, Height = 35, BackColor = Color.Transparent, NormalColor = Color.Transparent, HoverColor = Color.FromArgb(50, 50, 50), ForeColor = TextGrey, Font = new Font("Segoe UI", 10F), FlatStyle = FlatStyle.Flat };
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.BorderColor = TextGrey;
        btnCancel.Click += (s, e) => ClearAddForm();
        formCard.Controls.Add(btnCancel);
    }

    private void BuildReportsPage()
    {
        _pnlReports = new Panel { Dock = DockStyle.Fill };
        _pnlMain.Controls.Add(_pnlReports);

        var lblTitle = new Label { Text = "Reports & Analytics", Font = new Font("Segoe UI", 20F, FontStyle.Bold), Dock = DockStyle.Top, Height = 50 };
        _pnlReports.Controls.Add(lblTitle);

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
        
        filterPanel.Controls.Add(new Label { Text = "From Date:", Location = new Point(0, 15), AutoSize = true, ForeColor = TextGrey });
        _dtpReportFrom = new DateTimePicker { Location = new Point(100, 12), Width = 130, Format = DateTimePickerFormat.Short };
        _dtpReportFrom.Font = new Font("Segoe UI", 10F);
        filterPanel.Controls.Add(_dtpReportFrom);

        filterPanel.Controls.Add(new Label { Text = "To Date:", Location = new Point(250, 15), AutoSize = true, ForeColor = TextGrey });
        _dtpReportTo = new DateTimePicker { Location = new Point(330, 12), Width = 130, Format = DateTimePickerFormat.Short };
        _dtpReportTo.Font = new Font("Segoe UI", 10F);
        filterPanel.Controls.Add(_dtpReportTo);

        var btnFilter = new ModernButton { Text = "Apply Filter", Location = new Point(480, 10), Width = 120, Height = 30, BackColor = PrimaryGreen, NormalColor = PrimaryGreen, HoverColor = PrimaryGreenHover, ForeColor = TextWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        btnFilter.FlatAppearance.BorderSize = 0;
        btnFilter.Click += (s, e) => LoadReports();
        filterPanel.Controls.Add(btnFilter);
        
        _pnlReports.Controls.Add(filterPanel);
        filterPanel.BringToFront();

        var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 15, 0, 0) };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _pnlReports.Controls.Add(tlp);
        tlp.BringToFront();

        var cardBarChart = new CardPanel { Dock = DockStyle.Fill, BackColor = CardDark, Padding = new Padding(15), Margin = new Padding(0, 0, 5, 5) };
        cardBarChart.Controls.Add(new Label { Text = "Monthly Trend", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
        _chartReportsBar = new Chart { Dock = DockStyle.Fill, BackColor = CardDark };
        var chartAreaBar = new ChartArea("BarArea") { BackColor = CardDark };
        chartAreaBar.AxisX.LabelStyle.ForeColor = TextGrey;
        chartAreaBar.AxisY.LabelStyle.ForeColor = TextGrey;
        chartAreaBar.AxisX.LineColor = TextGrey;
        chartAreaBar.AxisY.LineColor = TextGrey;
        chartAreaBar.AxisX.MajorGrid.LineColor = Color.FromArgb(50, 50, 50);
        chartAreaBar.AxisY.MajorGrid.LineColor = Color.FromArgb(50, 50, 50);
        _chartReportsBar.ChartAreas.Add(chartAreaBar);
        _chartReportsBar.Series.Add(new Series("Monthly") { ChartType = SeriesChartType.Column, Color = SecondaryTeal, IsValueShownAsLabel = true, LabelForeColor = TextWhite });
        cardBarChart.Controls.Add(_chartReportsBar);
        _chartReportsBar.BringToFront();
        tlp.Controls.Add(cardBarChart, 0, 0);

        var cardPieChart = new CardPanel { Dock = DockStyle.Fill, BackColor = CardDark, Padding = new Padding(15), Margin = new Padding(5, 0, 0, 5) };
        cardPieChart.Controls.Add(new Label { Text = "Category-wise Spending", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
        _chartReportsPie = new Chart { Dock = DockStyle.Fill, BackColor = CardDark };
        var chartAreaPie = new ChartArea("PieArea") { BackColor = CardDark };
        _chartReportsPie.ChartAreas.Add(chartAreaPie);
        _chartReportsPie.Series.Add(new Series("Category") { ChartType = SeriesChartType.Pie, IsValueShownAsLabel = true, LabelForeColor = TextWhite });
        _chartReportsPie.Legends.Add(new Legend("Legend") { BackColor = CardDark, ForeColor = TextWhite });
        cardPieChart.Controls.Add(_chartReportsPie);
        _chartReportsPie.BringToFront();
        tlp.Controls.Add(cardPieChart, 1, 0);

        var cardLineChart = new CardPanel { Dock = DockStyle.Fill, BackColor = CardDark, Padding = new Padding(15), Margin = new Padding(0, 5, 0, 0) };
        cardLineChart.Controls.Add(new Label { Text = "Daily Spending Trend", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
        _chartReportsLine = new Chart { Dock = DockStyle.Fill, BackColor = CardDark };
        var chartAreaLine = new ChartArea("LineArea") { BackColor = CardDark };
        chartAreaLine.AxisX.LabelStyle.ForeColor = TextGrey;
        chartAreaLine.AxisY.LabelStyle.ForeColor = TextGrey;
        chartAreaLine.AxisX.LineColor = TextGrey;
        chartAreaLine.AxisY.LineColor = TextGrey;
        chartAreaLine.AxisX.MajorGrid.LineColor = Color.FromArgb(50, 50, 50);
        chartAreaLine.AxisY.MajorGrid.LineColor = Color.FromArgb(50, 50, 50);
        _chartReportsLine.ChartAreas.Add(chartAreaLine);
        var seriesLine = new Series("Daily") { ChartType = SeriesChartType.Line, Color = PrimaryGreen, BorderWidth = 3, IsValueShownAsLabel = false, MarkerStyle = MarkerStyle.Circle, MarkerSize = 8, MarkerColor = TextWhite };
        _chartReportsLine.Series.Add(seriesLine);
        cardLineChart.Controls.Add(_chartReportsLine);
        _chartReportsLine.BringToFront();
        tlp.Controls.Add(cardLineChart, 0, 1);
        tlp.SetColumnSpan(cardLineChart, 2);



        // Init default dates (last 6 months)
        _dtpReportFrom.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-5);
        _dtpReportTo.Value = DateTime.Now;
    }

    private void BuildCategoriesPage()
    {
        _pnlCategories = new Panel { Dock = DockStyle.Fill };
        _pnlMain.Controls.Add(_pnlCategories);

        var lblTitle = new Label { Text = "Manage Categories", Font = new Font("Segoe UI", 20F, FontStyle.Bold), Dock = DockStyle.Top, Height = 60 };
        _pnlCategories.Controls.Add(lblTitle);

        var cardCat = new CardPanel { Width = 400, Height = 400, BackColor = CardDark, Location = new Point(20, 70) };
        _pnlCategories.Controls.Add(cardCat);

        _lstCategories = new ListBox
        {
            Location = new Point(20, 20),
            Width = 360,
            Height = 250,
            BackColor = BgDark,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F)
        };
        cardCat.Controls.Add(_lstCategories);

        _txtNewCategory = new TextBox
        {
            Location = new Point(20, 290),
            Width = 240,
            BackColor = BgDark,
            ForeColor = TextWhite,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F)
        };
        cardCat.Controls.Add(_txtNewCategory);

        var btnAddCat = new ModernButton { Text = "Add", Location = new Point(270, 290), Width = 110, Height = 30, BackColor = PrimaryGreen, NormalColor = PrimaryGreen, HoverColor = PrimaryGreenHover, ForeColor = TextWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        btnAddCat.FlatAppearance.BorderSize = 0;
        btnAddCat.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(_txtNewCategory.Text))
            {
                _dbManager.AddCategory(_txtNewCategory.Text.Trim());
                _txtNewCategory.Text = "";
                LoadData();
            }
        };
        cardCat.Controls.Add(btnAddCat);

        var btnDelCat = new ModernButton { Text = "Delete Selected", Location = new Point(20, 340), Width = 360, Height = 35, BackColor = DangerRed, NormalColor = DangerRed, HoverColor = DangerRedHover, ForeColor = TextWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        btnDelCat.FlatAppearance.BorderSize = 0;
        btnDelCat.Click += (s, e) =>
        {
            if (_lstCategories.SelectedItem != null)
            {
                _dbManager.DeleteCategory(_lstCategories.SelectedItem.ToString());
                LoadData();
            }
        };
        cardCat.Controls.Add(btnDelCat);
    }

    private void LoadData()
    {
        var allExpenses = _dbManager.GetAllExpenses();
        decimal totalAll = allExpenses.Sum(e => e.Amount);
        
        var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        
        var monthExpenses = allExpenses.Where(e => e.Date.Year == currentMonthStart.Year && e.Date.Month == currentMonthStart.Month).ToList();
        decimal totalMonth = monthExpenses.Sum(e => e.Amount);

        _lblTotalAllTime.Text = $"{totalAll:C}";
        _lblTotalMonth.Text = $"{totalMonth:C}";

        // Categories
        if (_cbCategory != null)
        {
            var cats = _dbManager.GetCategories();
            string selCat = _cbCategory.SelectedItem?.ToString();
            _cbCategory.Items.Clear();
            _cbCategory.Items.AddRange(cats.ToArray());
            if (!string.IsNullOrEmpty(selCat) && cats.Contains(selCat)) _cbCategory.SelectedItem = selCat;
            else if (_cbCategory.Items.Count > 0) _cbCategory.SelectedIndex = 0;

            if (_lstCategories != null)
            {
                _lstCategories.Items.Clear();
                _lstCategories.Items.AddRange(cats.ToArray());
            }

            if (_cbSearchCategory != null)
            {
                string selSearch = _cbSearchCategory.SelectedItem?.ToString();
                _cbSearchCategory.Items.Clear();
                _cbSearchCategory.Items.Add("All Categories");
                foreach(var c in cats) _cbSearchCategory.Items.Add(c);
                if (!string.IsNullOrEmpty(selSearch) && _cbSearchCategory.Items.Contains(selSearch))
                    _cbSearchCategory.SelectedItem = selSearch;
                else
                    _cbSearchCategory.SelectedIndex = 0;
            }
        }

        // Dashboard DataGridView
        ApplyDashboardFilter();
        
        // Dashboard Chart (Pie)
        _chartDashPie.Series["Cats"].Points.Clear();
        var catGroups = monthExpenses.GroupBy(x => x.Category).Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) }).ToList();
        foreach (var c in catGroups)
        {
            var pt = _chartDashPie.Series["Cats"].Points.AddXY(c.Category, c.Total);
        }
        _chartDashPie.Update();

        // Reports Chart (Bar)
        if (_pnlReports.Visible) LoadReports();
    }

    private void ApplyDashboardFilter()
    {
        if (_dgvRecent == null || _cbSearchCategory == null || _cbSearchTime == null) return;
        
        var allExpenses = _dbManager.GetAllExpenses();
        var filteredExpenses = allExpenses;

        if (_cbSearchCategory.SelectedIndex > 0)
        {
            string cat = _cbSearchCategory.SelectedItem?.ToString();
            filteredExpenses = filteredExpenses.Where(e => e.Category == cat).ToList();
        }

        string timeFilter = _cbSearchTime.SelectedItem?.ToString() ?? "All Time";
        DateTime now = DateTime.Now.Date;
        
        if (timeFilter == "Today")
        {
            filteredExpenses = filteredExpenses.Where(e => e.Date.Date == now).ToList();
        }
        else if (timeFilter == "This Week")
        {
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            filteredExpenses = filteredExpenses.Where(e => e.Date.Date >= startOfWeek).ToList();
        }
        else if (timeFilter == "This Month")
        {
            filteredExpenses = filteredExpenses.Where(e => e.Date.Year == now.Year && e.Date.Month == now.Month).ToList();
        }

        _dgvRecent.DataSource = null;
        _dgvRecent.DataSource = filteredExpenses;
        if (_dgvRecent.Columns["Id"] != null) _dgvRecent.Columns["Id"].Visible = false;
    }

    private void LoadReports()
    {
        if (_dtpReportFrom == null || _dtpReportTo == null) return;
        
        DateTime fromDate = _dtpReportFrom.Value.Date;
        DateTime toDate = _dtpReportTo.Value.Date.AddDays(1).AddSeconds(-1); // inclusive of entire To-day
        
        var filteredExpenses = _dbManager.GetAllExpenses().Where(e => e.Date >= fromDate && e.Date <= toDate).ToList();

        // 1. Bar Chart: Monthly Trend
        _chartReportsBar.Series["Monthly"].Points.Clear();
        var monthlyGroups = filteredExpenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .ToList();

        foreach (var group in monthlyGroups)
        {
            DateTime monthDate = new DateTime(group.Key.Year, group.Key.Month, 1);
            decimal total = group.Sum(e => e.Amount);
            _chartReportsBar.Series["Monthly"].Points.AddXY(monthDate.ToString("MMM yy"), total);
        }
        _chartReportsBar.Update();

        // 2. Pie Chart: Category-wise spending
        if (_chartReportsPie != null)
        {
            _chartReportsPie.Series["Category"].Points.Clear();
            var categoryGroups = filteredExpenses
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList();

            foreach (var group in categoryGroups)
            {
                _chartReportsPie.Series["Category"].Points.AddXY(group.Category, group.Total);
            }
            _chartReportsPie.Update();
        }

        // 3. Line Chart: Daily Spending Trend
        if (_chartReportsLine != null)
        {
            _chartReportsLine.Series["Daily"].Points.Clear();
            var dailyGroups = filteredExpenses
                .GroupBy(e => e.Date.Date)
                .OrderBy(g => g.Key)
                .ToList();

            if (dailyGroups.Count > 0)
            {
                var dict = dailyGroups.ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
                var minDate = dailyGroups.First().Key;
                var maxDate = dailyGroups.Last().Key;
                if ((maxDate - minDate).TotalDays > 60) minDate = maxDate.AddDays(-60); // Cap at 60 days to avoid clutter
                
                for (var d = minDate; d <= maxDate; d = d.AddDays(1))
                {
                    decimal total = dict.ContainsKey(d) ? dict[d] : 0;
                    _chartReportsLine.Series["Daily"].Points.AddXY(d.ToString("MMM dd"), total);
                }
            }
            _chartReportsLine.Update();
        }
    }

    private void BtnAddExpenseSave_Click(object sender, EventArgs e)
    {
        if (!decimal.TryParse(_txtAmount.Text, out decimal amount) || amount <= 0)
        {
            MessageBox.Show("Please enter a valid amount.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var expense = new Expense
        {
            Date = _dtpDate.Value,
            Category = _cbCategory.Text,
            Amount = amount,
            Description = _txtDescription.Text
        };

        bool isEditing = _selectedExpenseId != -1;

        if (!isEditing)
        {
            _dbManager.AddExpense(expense);
            MessageBox.Show("Expense Added Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            expense.Id = _selectedExpenseId;
            _dbManager.UpdateExpense(expense);
            MessageBox.Show("Expense Updated Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        ClearAddForm();
        
        if (isEditing)
        {
            SwitchPage(_pnlDashboard, _btnNavDashboard);
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_dgvRecent.SelectedRows.Count == 0) return;
        int idToDelete = Convert.ToInt32(_dgvRecent.SelectedRows[0].Cells["Id"].Value);
        var res = MessageBox.Show("Delete this expense?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (res == DialogResult.Yes)
        {
            _dbManager.DeleteExpense(idToDelete);
            ClearAddForm();
            LoadData();
        }
    }

    private void ClearAddForm()
    {
        _selectedExpenseId = -1;
        _txtAmount.Text = "";
        _txtDescription.Text = "";
        if (_cbCategory.Items.Count > 0) _cbCategory.SelectedIndex = 0;
        _dtpDate.Value = DateTime.Now;
        if (_dgvRecent.Rows.Count > 0) _dgvRecent.ClearSelection();
    }

    // CUSTOM CONTROLS classes inside

    public class ModernButton : Button
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color HoverColor { get; set; } = Color.Gray;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color NormalColor { get; set; } = Color.DimGray;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.BackColor = HoverColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            // Restore normal color only if it doesn't have an active external overridden state
            // Let's implement active logic externally, so if normal color changes, update it
            this.BackColor = NormalColor;
        }
        
        // This allows changing normal color programmatically to keep active state
        public override Color BackColor 
        { 
            get => base.BackColor; 
            set 
            {
                base.BackColor = value;
                if (!DesignMode && !this.ClientRectangle.Contains(this.PointToClient(System.Windows.Forms.Cursor.Position)))
                    NormalColor = value;
            }
        }
    }

    public class CardPanel : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius { get; set; } = 15;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            using (var path = new GraphicsPath())
            {
                var r = new Rectangle(0, 0, this.Width, this.Height);
                int d = BorderRadius * 2;
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                
                this.Region = new Region(path); // This cuts off the sharp edges cleanly
                
                // Optionally draw border
                // using (var pen = new Pen(Color.FromArgb(40, 40, 40), 1))
                //     e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
