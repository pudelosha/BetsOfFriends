import { FormBuilder, FormGroup, Validators } from '@angular/forms';

export function buildMatchFormGroup(fb: FormBuilder, match: any): FormGroup {
  return fb.group({
    matchFrontendId: [match.matchFrontendId ?? null],
    matchId: [match.matchId ?? null],

    stage: [match.stage || ''],

    homeTeamId: [match.homeTeamId ?? null],
    homeTeamFrontendId: [match.homeTeamFrontendId ?? null],
    homeTeam: [match.homeTeam || '', Validators.required],

    awayTeamId: [match.awayTeamId ?? null],
    awayTeamFrontendId: [match.awayTeamFrontendId ?? null],
    awayTeam: [match.awayTeam || '', Validators.required],

    matchStart: [match.matchStart || '', Validators.required],

    matchType: [match.matchType || 'Regular90Min', Validators.required],

    homeWinOdds: [match.homeWinOdds, Validators.required],
    drawOdds: [match.drawOdds, Validators.required],
    awayWinOdds: [match.awayWinOdds, Validators.required],

    homeQualifies: [match.homeQualifies ?? null],
    awayQualifies: [match.awayQualifies ?? null],
  });
}
