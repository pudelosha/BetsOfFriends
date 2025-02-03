import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LogoffPage } from './logoff.page';

describe('LogoffPage', () => {
  let component: LogoffPage;
  let fixture: ComponentFixture<LogoffPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(LogoffPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
