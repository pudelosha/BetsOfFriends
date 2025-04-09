import { FormBuilder, FormGroup, Validators } from '@angular/forms';

export function buildMatchFormGroup(fb: FormBuilder, match: any): FormGroup {
  return fb.group({
    matchFrontendId: [match.matchFrontendId ?? null],
    matchId: [match.matchId ?? null],
    externalMatchId: [match.externalMatchId ?? null],

    stageFrontendId: [match.stageFrontendId ?? null, Validators.required],
    stageId: [match.stageId ?? null],
    stageName: [match.stageName || '', Validators.required],

    homeTeamFrontendId: [match.homeTeamFrontendId ?? null, Validators.required],
    homeTeamId: [match.homeTeamId ?? null],
    homeTeam: [match.homeTeam || '', Validators.required],

    awayTeamFrontendId: [match.awayTeamFrontendId ?? null, Validators.required],
    awayTeamId: [match.awayTeamId ?? null],
    awayTeam: [match.awayTeam || '', Validators.required],

    matchStart: [match.matchStart || '', Validators.required],
    matchType: [match.matchType || 'Regular90Min', Validators.required],

    homeWinOdds: [match.homeWinOdds ?? 1, Validators.required],
    drawOdds: [match.drawOdds ?? 1, Validators.required],
    awayWinOdds: [match.awayWinOdds ?? 1, Validators.required],

    homeQualifies: [match.homeQualifies ?? null],
    awayQualifies: [match.awayQualifies ?? null],

    matchStatus: [match.matchStatus ?? null],      
    scoreHome: [match.scoreHome ?? null],           
    scoreAway: [match.scoreAway ?? null],           

    isVisible: [match.isVisible ?? true],       
    recordStatus: [match.recordStatus ?? 'New'],
  });
}
