import {Component, OnDestroy, OnInit} from '@angular/core';
import { Subscription, debounceTime } from 'rxjs';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { DashboardService } from 'src/app/demo/service/dashboard.service';

@Component({
    templateUrl: './saas.dashboard.component.html'
})
export class SaaSDashboardComponent implements OnInit, OnDestroy {

    overviewChartData: any;

    overviewChartOptions: any;

    overviewWeeks: any;

    selectedOverviewWeek: any ;

    revenueChartData: any;

    revenueChartOptions: any;

    subscription: Subscription;

    todaySales = 0;
    todayPurchases = 0;
    todayExpenses = 0;
    todayProfit = 0;

    constructor(
        public layoutService: LayoutService,
        private dashboardService: DashboardService
    ) {
        this.subscription = this.layoutService.configUpdate$
        .pipe(debounceTime(25))
        .subscribe((config) => {
            this.initCharts();
        });
    }

    ngOnInit() {
        this.initCharts();
        this.loadDashboard();

        this.overviewWeeks = [
            {name: 'Last Week', code: '0'},
            {name: 'This Week', code: '1'}
        ];
        this.selectedOverviewWeek = this.overviewWeeks[0]
    }

    async loadDashboard() {
        try {
            const data = await this.dashboardService.get();
            this.todaySales = data.todaySales ?? 0;
            this.todayPurchases = data.todayPurchases ?? 0;
            this.todayExpenses = data.todayExpenses ?? 0;
            this.todayProfit = data.todayProfit ?? 0;
        } catch {
            this.todaySales = 0;
            this.todayPurchases = 0;
            this.todayExpenses = 0;
            this.todayProfit = 0;
        }
    }

    formatPkr(amount: number): string {
        const value = amount ?? 0;
        return (
            'PKR ' +
            value.toLocaleString('en-PK', {
                minimumFractionDigits: 0,
                maximumFractionDigits: 2,
            })
        );
    }

    kpiSubtitle(amount: number): string {
        return (amount ?? 0) === 0 ? 'No data today' : '';
    }

    get salesSubtitle(): string {
        return this.kpiSubtitle(this.todaySales);
    }

    get purchasesSubtitle(): string {
        return this.kpiSubtitle(this.todayPurchases);
    }

    get expensesSubtitle(): string {
        return this.kpiSubtitle(this.todayExpenses);
    }

    get profitSubtitle(): string {
        const noActivity =
            this.todaySales === 0 &&
            this.todayPurchases === 0 &&
            this.todayExpenses === 0;
        return noActivity ? 'No data today' : '';
    }

    initCharts() {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const primaryColor = documentStyle.getPropertyValue('--primary-color');
        const primaryColor300 = documentStyle.getPropertyValue('--primary-200');
        const borderColor = documentStyle.getPropertyValue('--surface-border');

        this.overviewChartData = {
            labels: ['M', 'T', 'W', 'T', 'F', 'S', 'S'],
            datasets: [
                {
                    label: 'Organic',
                    data: [2, 1, 0.5, 0.6, 0.5, 1.3, 1],
                    borderColor: [
                        primaryColor
                    ],
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
                    type: 'line',
                    fill: false,
                },
                {
                    label: 'Referral',
                    data: [4.88, 3, 6.2, 4.5, 2.1, 5.1, 4.1],
                    backgroundColor: [this.layoutService.config().colorScheme === 'dark' ? '#879AAF' : '#E4E7EB'] ,
                    hoverBackgroundColor: [primaryColor300],
                    fill: true,
                    borderRadius: 10,
                    borderSkipped: 'top bottom',
                    barPercentage: 0.3
                }
            ]
        };

        this.overviewChartOptions = {
            plugins: {
                legend: {
                    position: 'bottom',
                    align: 'end',
                    labels: {
                        color: textColorSecondary
                    }
                }
            },
            responsive: true,
            hover: {
                mode: 'index'
            },
            scales: {
                y: {
                    max: 7,
                    min: 0,
                    ticks: {
                        stepSize: 0,
                        callback: function(value: number, index: number) {
                            if (index === 0) {
                                return value;
                            }
                            else {
                                return value + 'k';
                            }
                        },
                        color: textColorSecondary
                    },
                    grid: {
                        borderDash: [2, 2],
                        color: borderColor,
                        drawBorder: false
                    }
                },
                x: {
                    grid: {
                        display: false,
                    },
                    ticks: {
                        beginAtZero: true,
                        color: textColorSecondary
                    }
                }
            }
        };

        this.revenueChartData = {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
            datasets: [
                {
                    data: [11, 17, 30, 60, 88, 92],
                    borderColor: 'rgba(25, 146, 212, 0.5)',
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
                    fill: false,
                    tension: .4
                },
                {
                    data: [11, 19, 39, 59, 69, 71],
                    borderColor: 'rgba(25, 146, 212, 0.5)',
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
                    fill: false,
                    tension: .4
                },
                {
                    data: [11, 17, 21, 30, 47, 83],
                    backgroundColor: 'rgba(25, 146, 212, 0.2)',
                    borderColor: 'rgba(25, 146, 212, 0.5)',
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
                    fill: true,
                    tension: .4
                }
            ]
        };

        this.revenueChartOptions = {
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    grid: {
                        color: borderColor
                    },
                    max: 100,
                    min: 0,
                    ticks: {
                        color: textColorSecondary
                    }
                },
                x: {
                    grid: {
                        color: borderColor
                    },
                    ticks: {
                        color: textColorSecondary,
                        beginAtZero: true
                    }
                }
            }
        };
    }

    changeOverviewWeek() {
        const dataSet1 = [
            [2, 1, 0.5, 0.6, 0.5, 1.3, 1],
            [4.88, 3, 6.2, 4.5, 2.1, 5.1, 4.1]
        ];
        const dataSet2 = [
            [3, 2.4, 1.5, 0.6, 4.5, 3.3, 2],
            [3.2, 4.1, 2.2, 5.5, 4.1, 3.6, 3.5],
        ];

        if (this.selectedOverviewWeek.code === '1') {
            this.overviewChartData.datasets[0].data = dataSet2[parseInt('0')];
            this.overviewChartData.datasets[1].data = dataSet2[parseInt('1')];
        } 
        else {
            this.overviewChartData.datasets[0].data = dataSet1[parseInt('0')];
            this.overviewChartData.datasets[1].data = dataSet1[parseInt('1')];
        }

        this.overviewChartData = {...this.overviewChartData};
    }

    get colorScheme(): string {
        return this.layoutService.config().colorScheme;
    }

    ngOnDestroy(): void {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
}
