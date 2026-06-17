import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { TournamentResultsChart, TournamentResultsChartPoint, TournamentResultsChartSeries, TournamentSummary } from 'src/app/model/tournament-model';
import { ModalController } from '@ionic/angular';
import { PlayerStatsModalComponent } from 'src/app/modals/player-stats-modal/player-stats-modal.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonGrid, IonRow, IonCol, IonProgressBar, IonSelect, IonSelectOption } from '@ionic/angular/standalone';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-tournament-results',
  templateUrl: './tournament-results.page.html',
  styleUrls: ['./tournament-results.page.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonContent, IonGrid, IonRow, IonCol, IonProgressBar, IonSelect, IonSelectOption],
})
export class TournamentResultsPage implements OnInit {
  tournamentId: number | null = null;
  summaryData: TournamentSummary[] = [];
  chartData: TournamentResultsChart | null = null;
  selectedChartUserIds: string[] = [];
  activeChartPoint: {
    series: TournamentResultsChartSeries;
    details: TournamentResultsChartPoint;
    x: number;
    y: number;
    pageX: number;
    pageY: number;
  } | null = null;
  activeChartLine: {
    series: TournamentResultsChartSeries;
    pageX: number;
    pageY: number;
  } | null = null;
  activeStackSegment: {
    label: string;
    points: number;
    pageX: number;
    pageY: number;
  } | null = null;
  isLoading = true;

  readonly chartWidth = 640;
  readonly chartHeight = 260;
  readonly chartPadding = { top: 18, right: 22, bottom: 40, left: 38 };
  readonly chartColors = ['#3767ff', '#54b96f', '#e78442', '#f0c735', '#9b75f1', '#23a6a6', '#e0527d', '#8a8f3a'];

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private modalController: ModalController,
    private titleService: TitleService,
    private translate: TranslateService
  ) {}

  async ngOnInit() {
    //await this.loadTournamentAndFetchSummary();
  }

  async ionViewWillEnter() {
    this.titleService.setTitle('RESULTS.TITLE');
    await this.loadTournamentAndFetchSummary();
  }

  private async loadTournamentAndFetchSummary() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (this.tournamentId === null) {
      await this.showToast(this.t('TOASTS.NO_TOURNAMENT_SELECTED'), 'warning');
      this.isLoading = false;
      return;
    }

    await this.fetchSummary();
  }

  async fetchSummary() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch results.');
      return;
    }
  
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING_RESULTS'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    forkJoin({
      summary: this.tournamentService.getTournamentSummary(this.tournamentId),
      chart: this.tournamentService.getTournamentResultsChart(this.tournamentId)
    }).subscribe({
      next: async ({ summary, chart }) => {
        this.summaryData = summary;
        this.chartData = chart;
        this.selectedChartUserIds = chart.series.map(series => series.userId);
        this.isLoading = false;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
        }, delay);
      },
      error: async (error) => {
        console.error('Error fetching results:', error);
        this.isLoading = false;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          await this.showToast(this.t('TOASTS.RESULTS_LOAD_FAILED'), 'danger');
        }, delay);
      },
    });
  }
  
  async openPlayerStats(userId: string) {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (!tournamentId) {
      console.error("No tournament selected.");
      return;
    }
  
    const modal = await this.modalController.create({
      component: PlayerStatsModalComponent,
      componentProps: { tournamentId, userId },
      breakpoints: [1],
      initialBreakpoint: 1
    });
  
    await modal.present();
  }

  get showQualifiedColumn(): boolean {
    return this.summaryData?.length ? this.summaryData[0].showQualified : false;
  }
  
  get showExactResultColumn(): boolean {
    return this.summaryData?.length ? this.summaryData[0].showExactResult : false;
  }
  
  calculatePlayerColumnSize(): number {
    const baseSize = 3.4;
    let extra = 0;
    if (!this.showQualifiedColumn) extra += 1;
    if (!this.showExactResultColumn) extra += 1;
    return baseSize + extra;
  }

  formatPositionChange(change?: number | null): string {
    if (!change) {
      return '-';
    }

    return change > 0 ? `▲${change}` : `▼${Math.abs(change)}`;
  }

  getPositionChangeClass(change?: number | null): string {
    if (!change) {
      return 'position-neutral';
    }

    return change > 0 ? 'position-up' : 'position-down';
  }

  getDisplayPosition(index: number): number {
    if (index <= 0) {
      return 1;
    }

    const current = this.summaryData[index];
    const previous = this.summaryData[index - 1];

    if (this.roundPoints(current?.totalPayout) === this.roundPoints(previous?.totalPayout)) {
      return this.getDisplayPosition(index - 1);
    }

    return index + 1;
  }

  private roundPoints(value?: number | null): number | null {
    return value === null || value === undefined ? null : Math.round(value * 100) / 100;
  }

  get hasChartData(): boolean {
    return !!this.chartData?.labels?.length && !!this.chartData?.series?.length;
  }

  get filteredChartSeries(): TournamentResultsChartSeries[] {
    const selected = new Set(this.selectedChartUserIds);
    return (this.chartData?.series ?? []).filter(series => selected.has(series.userId));
  }

  get hasStackedResultsData(): boolean {
    return this.summaryData.some(player => this.getPositiveTotalPayout(player) > 0);
  }

  get sortedAccuracyData(): TournamentSummary[] {
    return [...this.summaryData].sort((a, b) => {
      const accuracyDiff = (b.betSuccessRate ?? 0) - (a.betSuccessRate ?? 0);
      if (accuracyDiff !== 0) {
        return accuracyDiff;
      }

      return (b.totalPayout ?? 0) - (a.totalPayout ?? 0);
    });
  }

  get hasAccuracyChartData(): boolean {
    return this.summaryData.some(player => (player.betSuccessRate ?? 0) > 0 || (player.previousBetSuccessRate ?? 0) > 0);
  }

  get maxStackedTotalPayout(): number {
    return Math.max(0, ...this.summaryData.map(player => this.getPositiveTotalPayout(player)));
  }

  getStackSegmentWidth(player: TournamentSummary, value: number | null | undefined): number {
    const maxTotal = this.maxStackedTotalPayout;
    if (!maxTotal || !value || value <= 0) {
      return 0;
    }

    return value / maxTotal * 100;
  }

  showStackSegment(labelKey: string, points: number | null | undefined, event: MouseEvent): void {
    const horizontalMargin = Math.min(120, Math.max(36, window.innerWidth / 2 - 12));
    const verticalMargin = Math.min(70, Math.max(28, window.innerHeight / 4));

    this.activeStackSegment = {
      label: this.t(labelKey),
      points: points ?? 0,
      pageX: Math.min(Math.max(event.clientX, horizontalMargin), window.innerWidth - horizontalMargin),
      pageY: Math.min(Math.max(event.clientY - 14, verticalMargin), window.innerHeight - verticalMargin)
    };
  }

  clearStackSegment(): void {
    this.activeStackSegment = null;
  }

  private getPositiveTotalPayout(player: TournamentSummary): number {
    return Math.max(0, player.regularPayout ?? 0) +
      Math.max(0, player.qualificationPayout ?? 0) +
      Math.max(0, player.exactScorePayout ?? 0);
  }

  getAccuracyBaseWidth(player: TournamentSummary): number {
    return this.clampPercent(player.betSuccessRate);
  }

  private clampPercent(value?: number | null): number {
    return Math.min(100, Math.max(0, value ?? 0));
  }

  get chartMaxValue(): number {
    const values = this.filteredChartSeries.reduce<number[]>(
      (allPoints, series) => allPoints.concat(series.points),
      []
    );
    const max = Math.max(0, ...values);
    return max <= 0 ? 1 : Math.ceil(max);
  }

  get chartTicks(): number[] {
    const max = this.chartMaxValue;
    const mid = Math.round((max / 2) * 100) / 100;
    return [max, mid, 0];
  }

  get chartPlotWidth(): number {
    return this.chartWidth - this.chartPadding.left - this.chartPadding.right;
  }

  get chartPlotHeight(): number {
    return this.chartHeight - this.chartPadding.top - this.chartPadding.bottom;
  }

  getSeriesColor(index: number): string {
    return this.chartColors[index % this.chartColors.length];
  }

  getFilteredSeriesColor(series: TournamentResultsChartSeries): string {
    const originalIndex = this.chartData?.series?.findIndex(item => item.userId === series.userId) ?? 0;
    return this.getSeriesColor(Math.max(0, originalIndex));
  }

  onChartParticipantsChange(value: string[] | string | null | undefined): void {
    this.selectedChartUserIds = Array.isArray(value) ? value : value ? [value] : [];
    this.clearChartPoint();
    this.clearChartLine();
  }

  getPointX(index: number): number {
    const count = this.chartData?.labels?.length ?? 0;
    if (count <= 1) {
      return this.chartPadding.left + this.chartPlotWidth / 2;
    }

    return this.chartPadding.left + (index / (count - 1)) * this.chartPlotWidth;
  }

  getPointY(value: number): number {
    return this.chartPadding.top + this.chartPlotHeight - (value / this.chartMaxValue) * this.chartPlotHeight;
  }

  getLinePath(series: TournamentResultsChartSeries): string {
    return series.points
      .map((point, index) => `${index === 0 ? 'M' : 'L'} ${this.getPointX(index)} ${this.getPointY(point)}`)
      .join(' ');
  }

  getTickY(value: number): number {
    return this.getPointY(value);
  }

  showChartLine(series: TournamentResultsChartSeries, event: MouseEvent): void {
    const horizontalMargin = Math.min(120, Math.max(36, window.innerWidth / 2 - 12));
    const verticalMargin = Math.min(80, Math.max(28, window.innerHeight / 4));

    this.activeChartLine = {
      series,
      pageX: Math.min(Math.max(event.clientX, horizontalMargin), window.innerWidth - horizontalMargin),
      pageY: Math.min(Math.max(event.clientY - 18, verticalMargin), window.innerHeight - verticalMargin)
    };
  }

  clearChartLine(): void {
    this.activeChartLine = null;
  }

  showChartPoint(series: TournamentResultsChartSeries, pointIndex: number, event?: MouseEvent | TouchEvent): void {
    const details = series.pointDetails[pointIndex];
    if (!details) {
      return;
    }

    this.clearChartLine();

    const pointer = event instanceof TouchEvent ? event.touches[0] : event;
    const clientX = pointer?.clientX ?? window.innerWidth / 2;
    const clientY = pointer?.clientY ?? window.innerHeight / 2;
    const horizontalMargin = Math.min(150, Math.max(24, window.innerWidth / 2 - 12));
    const verticalMargin = Math.min(125, Math.max(80, window.innerHeight / 3));

    this.activeChartPoint = {
      series,
      details,
      x: this.getPointX(pointIndex),
      y: this.getPointY(series.points[pointIndex] ?? 0),
      pageX: Math.min(Math.max(clientX, horizontalMargin), window.innerWidth - horizontalMargin),
      pageY: Math.min(Math.max(clientY, verticalMargin), window.innerHeight - verticalMargin)
    };
  }

  clearChartPoint(): void {
    this.activeChartPoint = null;
  }

  getPointsDeltaClass(value: number): string {
    return value > 0 ? 'points-positive' : 'points-zero';
  }

  getChartMatchStatus(result: string, bet: string): string {
    const actual = this.parseScore(result);
    const predicted = this.parseScore(bet);

    if (!predicted) {
      return this.t('MY_BETS_FINALISED.STATUS_NOT_PREDICTED');
    }

    if (!actual) {
      return this.t('MY_BETS_FINALISED.STATUS_NOT_FINALIZED');
    }

    if (predicted.home === actual.home && predicted.away === actual.away) {
      return this.t('MY_BETS_FINALISED.STATUS_EXACT_MATCH');
    }

    const predictedOutcome = this.getScoreOutcome(predicted.home, predicted.away);
    const actualOutcome = this.getScoreOutcome(actual.home, actual.away);

    return predictedOutcome === actualOutcome
      ? this.t('MY_BETS_FINALISED.STATUS_WON')
      : this.t('MY_BETS_FINALISED.STATUS_LOST');
  }

  private parseScore(score: string): { home: number; away: number } | null {
    const match = /^(\d+):(\d+)$/.exec(score?.trim() ?? '');
    if (!match) {
      return null;
    }

    return {
      home: Number(match[1]),
      away: Number(match[2])
    };
  }

  private getScoreOutcome(home: number, away: number): 'home' | 'away' | 'draw' {
    if (home > away) {
      return 'home';
    }

    if (home < away) {
      return 'away';
    }

    return 'draw';
  }
          
  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
