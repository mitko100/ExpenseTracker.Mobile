﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ExpenseTracker.Mobile.Events;
using ExpenseTracker.Mobile.Extensions;
using ExpenseTracker.Mobile.Factories;
using ExpenseTracker.Mobile.Services;
using ExpenseTracker.Mobile.Utils;
using ExpenseTracker.Mobile.ViewModels.Helpers;
using Prism.Events;
using Prism.Mvvm;
using Xamarin.Forms;

namespace ExpenseTracker.Mobile.ViewModels
{
    public class CalendarControlViewModel : BindableBase
    {
        private const int WeekDaysCount = 7;
        private readonly IDateTimeService dateTimeService;
        private readonly IViewModelsFactory viewModelsFactory;
        private readonly IEventAggregator eventAggregator;

        private int selectedPosition;
        public int SelectedPosition
        {
            get => this.selectedPosition;
            set => this.SetProperty(ref this.selectedPosition, value);
        }

        public TextUI Month { get; set; }

        public ButtonUI TodayButton { get; set; }

        public CollectionUI<WeekViewModel> Weeks { get; set; }

        public ICommand PositionSelectedCommand { get; set; }

        public CalendarControlViewModel(
            IDateTimeService dateTimeService, 
            IViewModelsFactory viewModelsFactory,
            IEventAggregator eventAggregator)
        {
            this.dateTimeService = dateTimeService;
            this.viewModelsFactory = viewModelsFactory;
            this.eventAggregator = eventAggregator;
            this.Initialize();
        }       

        private void Initialize()
        {
            this.Month = new TextUI() { Text = this.dateTimeService.UtcNow.GetMonth()  };
            this.TodayButton = new ButtonUI(new Command(this.OnTodayButtonClicked));

            var weekDates = DateTimeUtils.GetCurrentWeek(this.dateTimeService);

            var prevWeek = this.viewModelsFactory.Create(weekDates.Select(x => x.AddDays(-WeekDaysCount)).ToList());
            prevWeek.SelectedDateChanged += this.OnSelectedDateChanged;

            var currentWeek = this.viewModelsFactory.Create(weekDates);
            currentWeek.SelectedDateChanged += this.OnSelectedDateChanged;

            var nextWeek = this.viewModelsFactory.Create(weekDates.Select(x => x.AddDays(WeekDaysCount)).ToList());
            nextWeek.SelectedDateChanged += this.OnSelectedDateChanged;

            this.Weeks = new CollectionUI<WeekViewModel>(new List<WeekViewModel>
            {
                prevWeek, currentWeek, nextWeek
            });

            this.SelectedPosition = 1;
            this.PositionSelectedCommand = new Command(this.OnPositonSelected);
        }

        private void OnTodayButtonClicked(object obj)
        {
            var today = this.dateTimeService.UtcNow;
            var week = this.Weeks.Items
                .Where(x => x.StartDate <= today && today <= x.EndDate)
                .FirstOrDefault();

            var index = this.Weeks.Items.IndexOf(week);
            this.SelectedPosition = index;

            week.DayClickedCommand.Execute(week.GetDay(today.DayOfWeek));
        }

        private void OnSelectedDateChanged(object sender, DateTime e)
        {
            this.Month.Text = e.GetMonth();

            this.eventAggregator.GetEvent<DateSelectedEvent>().Publish(e);
        }

        private void OnPositonSelected(object obj)
        {
            if (this.SelectedPosition == 0)
            {
                var lastWeek = this.Weeks.Items.First();
                
                var prevWeek = Enumerable.Range(1, 7)
                    .Select(x => lastWeek.StartDate.AddDays(-x))
                    .OrderBy(x => x.Date)
                    .ToList();

                var weekVM = this.viewModelsFactory.Create(prevWeek);
                weekVM.SelectedDateChanged += this.OnSelectedDateChanged;

                this.Weeks.Items.Insert(0, weekVM);
            }

            if (this.SelectedPosition == this.Weeks.Items.Count - 1)
            {
                var nextWeek = this.Weeks.Items.Last();
                var prevWeek = Enumerable.Range(1, 7)
                    .Select(x => nextWeek.EndDate.AddDays(x))
                    .OrderBy(x => x.Date)
                    .ToList();

                var weekVM = this.viewModelsFactory.Create(prevWeek);
                weekVM.SelectedDateChanged += this.OnSelectedDateChanged;

                this.Weeks.Items.Add(weekVM);
            }
        }
    }
}
