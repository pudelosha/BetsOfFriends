import { FormBuilder, FormGroup, Validators } from '@angular/forms';

export function buildMatchFormGroup(fb: FormBuilder, match: any): FormGroup {
  return fb.group({
    matchFrontendId: [match.matchFrontendId ?? null],
    matchId: [match.matchId ?? null],

    stageFrontendId: [match.stageFrontendId ?? null, Validators.required], // Ensure valid stage is selected
    stageId: [match.stageId ?? null],
    stageName: [match.stageName || '', Validators.required], // Ensure name consistency

    homeTeamFrontendId: [match.homeTeamFrontendId ?? null, Validators.required], // Always set for frontend tracking
    homeTeamId: [match.homeTeamId ?? null],
    homeTeam: [match.homeTeam || '', Validators.required],

    awayTeamFrontendId: [match.awayTeamFrontendId ?? null, Validators.required], // Always set for frontend tracking
    awayTeamId: [match.awayTeamId ?? null],
    awayTeam: [match.awayTeam || '', Validators.required],

    matchStart: [match.matchStart || '', Validators.required],

    matchType: [match.matchType || 'Regular90Min', Validators.required],

    homeWinOdds: [match.homeWinOdds, Validators.required],
    drawOdds: [match.drawOdds, Validators.required],
    awayWinOdds: [match.awayWinOdds, Validators.required],

    homeQualifies: [match.homeQualifies ?? null],
    awayQualifies: [match.awayQualifies ?? null],

    recordStatus: [match.recordStatus ?? 'New'],
  });
}
