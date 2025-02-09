import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PredefinedTournamentsPage } from './predefined-tournaments.page';

describe('PredefinedTournamentsPage', () => {
  let component: PredefinedTournamentsPage;
  let fixture: ComponentFixture<PredefinedTournamentsPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(PredefinedTournamentsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
