import { FormBuilder, FormGroup, Validators } from '@angular/forms';

export function buildMatchFormGroup(fb: FormBuilder, match: any): FormGroup {
  return fb.group({
    matchId: [match.matchId || null], // Preserve ID or set to null
    homeTeamId: [match.homeTeamId || null], // Preserve ID or set to null
    awayTeamId: [match.awayTeamId || null], // Preserve ID or set to null
    stage: [match.stage || ''], // No Validators.required
    homeTeam: [match.homeTeam || '', Validators.required],
    awayTeam: [match.awayTeam || '', Validators.required],
    matchStart: [match.matchStart || '', Validators.required],
    betType: [match.betType || '90min', Validators.required], // Default value
    homeWinOdds: [match.homeWinOdds || null, Validators.required],
    drawOdds: [match.drawOdds || null, Validators.required],
    awayWinOdds: [match.awayWinOdds || null, Validators.required],
    homeQualifies: [match.homeQualifies || null], // Nullable field
    awayQualifies: [match.awayQualifies || null], // Nullable field
  });
}
