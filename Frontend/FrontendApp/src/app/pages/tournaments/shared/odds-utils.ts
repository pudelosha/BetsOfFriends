export interface EloMatchOdds {
  homeWinOdds: number;
  drawOdds: number;
  awayWinOdds: number;
}

export interface EloQualificationOdds {
  homeQualifies: number;
  awayQualifies: number;
}

interface EloProbabilities {
  homeWin: number;
  draw: number;
  awayWin: number;
}

const HOME_ADVANTAGE_ELO = 50;
const ELO_DIVISOR = 600;

const round2 = (value: number): number => Math.round(value * 100) / 100;
const clamp = (value: number, min: number, max: number): number => Math.max(min, Math.min(max, value));

function calculateEloProbabilities(
  homeElo: number,
  awayElo: number,
  includeHomeAdvantage: boolean
): EloProbabilities | null {
  const homeAdvantage = includeHomeAdvantage ? HOME_ADVANTAGE_ELO : 0;
  const powerRatio = 1 / (1 + Math.pow(10, (awayElo - homeElo - homeAdvantage) / ELO_DIVISOR));

  const draw = clamp(0.29 - Math.abs(0.5 - powerRatio) * 0.3, 0, 0.33);
  const homeWin = (1 - draw) * powerRatio;
  const awayWin = 1 - homeWin - draw;

  if (!(homeWin > 0) || !(draw > 0) || !(awayWin > 0)) return null;

  return { homeWin, draw, awayWin };
}

export function calculateEloMatchOdds(
  homeElo: number,
  awayElo: number,
  includeHomeAdvantage: boolean
): EloMatchOdds | null {
  const probabilities = calculateEloProbabilities(homeElo, awayElo, includeHomeAdvantage);
  if (!probabilities) return null;

  const homeAwayJitter = 0.95 + Math.random() / 100;
  const drawJitter = 0.95 + Math.random() / 100;

  return {
    homeWinOdds: round2((1 / probabilities.homeWin) * homeAwayJitter),
    drawOdds: round2((1 / probabilities.draw) * drawJitter),
    awayWinOdds: round2((1 / probabilities.awayWin) * homeAwayJitter),
  };
}

export function calculateEloQualificationOdds(
  homeElo: number,
  awayElo: number,
  includeHomeAdvantage: boolean
): EloQualificationOdds | null {
  const probabilities = calculateEloProbabilities(homeElo, awayElo, includeHomeAdvantage);
  if (!probabilities) return null;

  const homeQualifiesProbability = probabilities.homeWin + probabilities.draw * 0.5;
  const awayQualifiesProbability = 1 - homeQualifiesProbability;

  if (!(homeQualifiesProbability > 0) || !(awayQualifiesProbability > 0)) return null;

  const homeJitter = 0.95 + Math.random() / 100;
  const awayJitter = 0.95 + Math.random() / 100;

  return {
    homeQualifies: round2((1 / homeQualifiesProbability) * homeJitter),
    awayQualifies: round2((1 / awayQualifiesProbability) * awayJitter),
  };
}
