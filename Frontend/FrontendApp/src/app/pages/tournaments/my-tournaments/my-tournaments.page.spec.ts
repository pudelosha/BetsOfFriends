import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyTournamentsPage } from './my-tournaments.page';

describe('MyTournamentsPage', () => {
  let component: MyTournamentsPage;
  let fixture: ComponentFixture<MyTournamentsPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(MyTournamentsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
